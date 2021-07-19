using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class Index : IDisposable
    {
        private Logger _logger;
        private static readonly DbConnection Db = new();
        private static List<LandingData> _landed = new();
        private static List<CoilData> _scaled = new();
        private static List<IngotsInOven> _meltsInOven = new();
        
        private Timer _timer;
        private DateTime _lastQueryTime;
        private DateTime _lastPausedTime;
        private TimeSpan _workInterval;
        private TimeSpan _pauseInterval;
        
        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";
        private string _semaphoreColor = "#1861ac";
        private string _selectRow = "none";
        private string _outFromOven = "none";
        private string _selectedColor = "#e0ffff";
        private string _loading = "hidden;";
        private bool _movingButtonsState = true;
        private bool _countingButtonsState = true;

        private int _landingId;
        private LandingRemap _remap = new();
        private List<string> _customers = new();
        private List<string> _standards = new();
        
        protected override void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            _landingService.PropertyChanged += UpdateMessage;
            Initialize();
        }

        public void Dispose()
        {
            _landingService.PropertyChanged -= UpdateMessage;
        }
        
        private void _setLoading(bool visible)
        {
            _loading = visible ? "visible;" : "hidden;";
        }

        private void ShowMessage(MessageType type, string message)
        {
            _message = message ?? "";
            switch (type)
            {
                case MessageType.Success: _messageClass = "success"; break;
                case MessageType.Danger: _messageClass = "danger"; break;
                case MessageType.Warning: _messageClass = "warning"; break;
                case MessageType.Info: _messageClass = "info"; break;
            }

            _messageVisible = "inherit;";
        }

        private void HideMessage()
        {
            _message = "";
            _messageVisible = "none;";
        }

        private async void Initialize()
        {
            // Получение очереди плавок
            _setLoading(true);
            ShowMessage(MessageType.Info, "Получение данных");
            
            _landed = new List<LandingData>();
            _lastQueryTime = DateTime.Now;
            _lastPausedTime = DateTime.MinValue;
            _workInterval = TimeSpan.FromMinutes(5);
            _pauseInterval = TimeSpan.FromMinutes(1);
            _landingId = 0;
            
            await Task.Delay(100);
            
            _landed = GetLandingOrder();
            
            _setLoading(false);
            HideMessage();
            StateHasChanged();
            SetTimer(20);
        }

        /// <summary>
        /// Добавить заготовку к посаду по идентификатору посада
        /// </summary>
        /// <param name="uid">Идентификатор посада</param>
        private async void IncLanding(int uid)
        {
            string meltNo = "";
            int oldCnt = 0;
            bool canAdd = false;

            foreach (LandingData melt in _landed)
            {
                if (melt.LandingId == uid)
                {
                    meltNo = melt.MeltNumber;
                    oldCnt = melt.IngotsCount;
                    canAdd = melt.IngotsCount - melt.WeightedIngots > 0;
                    break;
                }
            }

            _countingButtonsState = false;
            ShowMessage(MessageType.Warning, $"Добавление заготовки к плавке №{meltNo}");
            _setLoading(true);
            await Task.Delay(200);
            StateHasChanged();

            _logger.Info($"===== Начато добавление ЕУ к плавке [{uid}] №{meltNo} =====");

            // Если все заготовки в этой плавке уже взвесились, то добавлять заготовки на тест нельзя
            if (canAdd)
            {
                Db.IncLanding(uid);
                
                // Получим идент плавки на тесте по иденту плавки на проде
                int testUid = Db.GetTestLandingId(uid);
                Db.IncLanding(testUid, true);
            }

            _landed = GetLandingOrder(true);
            int newCnt = 0;
            
            foreach (LandingData melt in _landed)
            {
                if (melt.LandingId == uid)
                {
                    newCnt = melt.IngotsCount;
                    break;
                }
            }

            ShowMessage(MessageType.Success, $"Добавлена заготовка в плавку №{meltNo}");
            _logger.Info(
                $"Добавлена заготовка в плавку [{uid}] №{meltNo}. Количество заготвок: [{oldCnt}] => [{newCnt}]");
            await Task.Delay(5000);
            HideMessage();
            await Task.Delay(200);
            StateHasChanged();

            _countingButtonsState = true;
            _setLoading(false);
            StateHasChanged();
            _logger.Info($"===== Завершено добавление ЕУ к плавке [{uid}] №{meltNo} =====");
        }

        /// <summary>
        /// Удалить заготовоку из посада по идентификатору посада
        /// </summary>
        /// <param name="uid">Идентификатр посада</param>
        private async void DecLanding(int uid)
        {
            string meltNo = "";
            int oldCnt = 0;
            bool canDeleted = false;
            
            foreach (LandingData melt in _landed)
            {
                if (melt.LandingId == uid)
                {
                    meltNo = melt.MeltNumber;
                    oldCnt = melt.IngotsCount;
                    canDeleted = melt.IngotsCount - melt.WeightedIngots > 0;
                    break;
                }
            }

            _countingButtonsState = false;
            ShowMessage(MessageType.Warning, $"Удаление заготовки из плавки №{meltNo}");
            _setLoading(true);
            await Task.Delay(200);
            StateHasChanged();

            _logger.Info($"===== Начато удаление ЕУ из плавки [{uid}] №{meltNo} =====");

            // Если количество взвешенных заготовок равно количеству заготовок в плавке
            // то не удаляем заготовку на тесте
            if (canDeleted)
            {
                Db.DecLanding(uid);
                
                // Получим идент плавки на тесте по иденту плавки на проде
                int testUid = Db.GetTestLandingId(uid);
                Db.DecLanding(testUid, true);
            }

            _landed = GetLandingOrder(true);
            int newCnt = 0;
            foreach (LandingData melt in _landed)
            {
                if (melt.LandingId == uid)
                {
                    newCnt = melt.IngotsCount;
                    break;
                }
            }

            ShowMessage(MessageType.Success, $"Удалена заготовка из плавки №{meltNo}");
            _logger.Info(
                $"Удалена заготовка из плавки [{uid}] №{meltNo}. Количество заготовок: [{oldCnt}] => [{newCnt}]");
            await Task.Delay(5000);
            HideMessage();
            await Task.Delay(200);
            StateHasChanged();

            _countingButtonsState = true;
            _setLoading(false);
            StateHasChanged();
            _logger.Info($"===== Завершено удаление ЕУ из плавки [{uid}] №{meltNo} =====");
        }

        /// <summary>
        /// Перемещение текущей плавки вверх (дальше от печи)
        /// </summary>
        /// <param name="uid">Идентификатор перемещаемой плавки</param>
        private async void MoveUp(int uid)
        {
            // Проходим по списку и ищем плавку с требуемым номером
            // Если количество взвешенных заготовок равно нулю
            // Если найденная плавка не первая в списке, и количество взвешенных заготовок у предыдущей плавки равно 0
            // Меняем местами выбранную плавку и предыдущую в списке
            
            _movingButtonsState = false;
            string meltNo = "";
            _setLoading(true);
            bool found = false;
            bool ordered = false;
            List<LandingData> order = new List<LandingData>();

            // 1. Получаем текущий список плавок
            foreach (LandingData melt in _landed)
            {
                // Ограничение списка плавок на этапе отбора
                if(melt.WeightedIngots==0 && melt.Weighted == 0)
                {
                    order.Add(melt);
                    if (melt.LandingId == uid)
                        meltNo = melt.MeltNumber;
                }
            }
            
            ShowMessage(MessageType.Warning, $"Перемещение плавки №{meltNo} вверх");
            await Task.Delay(100);
            StateHasChanged();

            // 2. Ищем выбранную плавку в очереди
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].LandingId == uid)
                {
                    // Нашли плавку
                    meltNo = order[i].MeltNumber;
                    _logger.Info($"===== Начало перемещения вверх по очереди для плавки [{uid}] №{meltNo} =====");
                    found = true;
                    if (i > 0)
                    {
                        // Это не самая верхняя плавка
                        if (order[i].WeightedIngots == 0 && order[i].Weighted == 0)
                        {
                            // Нет взвешенных заготовок
                            if (order[i - 1].WeightedIngots == 0 && order[i - 1].Weighted == 0)
                            {
                                // Предыдущая плавка не имеет взвешенных заготовок
                                LandingData tmp = order[i];
                                order[i] = order[i - 1];
                                order[i - 1] = tmp;
                                ordered = true;
                                break;
                            }

                            ShowMessage(MessageType.Danger, $"Предыдущая плавка №{order[i - 1].MeltNumber} имеет взвешенные заготовки!");
                            _logger.Error(
                                $"Предыдущая плавка [{order[i - 1].LandingId}] №{order[i - 1].MeltNumber} имеет взвешенные заготовки! Нельзя поднять вверх!");
                            await Task.Delay(5000);
                            HideMessage();
                        }
                        else
                        {
                            ShowMessage(MessageType.Danger, $"Предыдущая плавка №{order[i - 1].MeltNumber} имеет взвешенные заготовки!");
                            _logger.Error(
                                $"Плавка [{uid}] №{order[i].MeltNumber} имеет взвешенные заготовки, нельзя поднять вверх!");
                            await Task.Delay(5000);
                            HideMessage();
                        }
                    }
                    else
                    {
                        ShowMessage(MessageType.Danger, $"Плавка №{order[i].MeltNumber} самая верхняя");
                        _logger.Error($"Плавка [{uid}] №{order[i].MeltNumber} самая верхняя, нельзя поднять вверх!");
                        await Task.Delay(5000);
                        HideMessage();
                    }
                }
            }

            if (!found)
            {
                ShowMessage(MessageType.Danger, $"Плавка с идентификатором {uid} не найдена в очереди");
                _logger.Error($"Плавка с идентификатором {uid} не найдена в очереди!");
                await Task.Delay(5000);
                HideMessage();
            }
            
            if(ordered)
            {
                int newCnt;
                int oldCnt;

                // Пока количество удаленных плавок не будет равно количеству добавленных или не более 5 попыток
                int repeat = 1;
                do
                {
                    _logger.Info($">>>>> Попытка №{repeat}");
                    
                    // Очистить текущую очередь
                    _logger.Info($"Плавка [{uid}] => Начата очистка текущей очереди");
                    oldCnt = ClearCurrentOrder();
                    _logger.Info($"Плавка [{uid}] => Завершена очистка текущей очереди");

                    // Заполнить новую очередь
                    _logger.Info($"Плавка [{uid}] => Начато заполнение нового порядка очереди. Удалено плавок [{oldCnt}]");
                    newCnt = SetNewOrder(order);
                    _logger.Info($"Плавка [{uid}] => Завершено заполнение нового порядка очереди. Добавлено плавок [{newCnt}]");
                    // newCnt = _landed.Count;
                    repeat++;
                } while (oldCnt != newCnt && repeat < 6);

                if (repeat == 5)
                    _logger.Error($">>>>> Ошибка при перемещении плавки [{uid}] ${meltNo} вверх");
            }

            _movingButtonsState = true;
            _setLoading(false);
            ShowMessage(MessageType.Success, $"Плавка №{meltNo} перемещена вверх по очереди");
            _logger.Info($"===== Завершение перемещения вверх по очереди для плавки [{uid}] №{meltNo} =====");
            await Task.Delay(5000);
            HideMessage();
            StateHasChanged();
        }

        /// <summary>
        /// Получить список плавок в очереди
        /// </summary>
        /// <param name="permanent">Немедленный запрос</param>
        /// <returns></returns>
        private List<LandingData> GetLandingOrder(bool permanent=false)
        {
            DateTime now = DateTime.Now;
            List<LandingData> result = new List<LandingData>();
            
            // Проверка времени последнего запроса
            if (_lastQueryTime == DateTime.MinValue)
            {
                _lastQueryTime = now;
            }
            else
            {
                if ( (now - _lastQueryTime <= _workInterval) || permanent )
                {
                    result = Db.GetLandingOrder();

                    // Проверка на наличие возвожности удаления плавки
                    foreach (LandingData item in result)
                    {
                        // int testUid = Db.GetTestLandingId(item.LandingId);
                        // WeightedIngotsCount weighted = Db.GetWeightedIngotsCount(testUid);
                        // item.WeightedIngots = weighted.WeightedCount;

                        item.DisplayProfile = _getDiameter(item.ProductProfile, item.Diameter);
                        
                        if (item.Weighted == 0 && item.WeightedIngots == 0)
                        {
                            item.CanBeDeleted = true;
                        }
                    }

                    _scaled = Db.GetIngotOnScales();
                    foreach (CoilData coil in _scaled)
                    {
                        coil.DisplayDiameter = _getDiameter(coil.ProductionProfile, coil.Diameter);
                    }
                    
                    _meltsInOven = Db.GetMeltsInOven();
                    foreach (IngotsInOven melt in _meltsInOven)
                    {
                        melt.DisplayDiameter = _getDiameter(melt.ProductProfile, melt.Diameter);
                    }
                }
                else
                {
                    // АРМ проработал один час или более, требуется пауза в 1 минуту
                    // _logger.Warn($"Работаем слишком долго [с {_lastQueryTime:G} {_workInterval:c}], требуется пауза");
                    
                    if (_lastPausedTime == DateTime.MinValue)
                        _lastPausedTime = now;

                    if (now - _lastPausedTime >= _pauseInterval)
                    {
                        _lastQueryTime = now;
                    }

                    result = _landed;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Перемещение текущей плавки вниз (ближе к печи)
        /// </summary>
        /// <param name="uid">Идентификатор перемещаемой плавки</param>
        private async void MoveDown(int uid)
        {
            // Проходим по списку и ищем плавку с требуемым номером
            // Если количество взвешенных заготовок равно нулю
            // Если найденная плавка не последняя в списке, и количество взвешенных заготовок у следующей плавки равно 0
            // Меняем местами выбранную плавку и следующую в списке
            
            _movingButtonsState = false;
            _setLoading(true);
            bool found = false;
            bool ordered = false;
            string meltNo = "";
            List<LandingData> order = new List<LandingData>();

            // 1. Получаем текущий список плавок
            foreach (LandingData melt in _landed)
            {
                // Ограничение списка плавок на этапе отбора
                if (melt.WeightedIngots == 0 && melt.Weighted == 0)
                {
                    order.Add(melt);
                    if (melt.LandingId == uid)
                        meltNo = melt.MeltNumber;
                }
            }
            
            ShowMessage(MessageType.Warning, $"Перемещение плавки №{meltNo} вниз");
            await Task.Delay(200);
            StateHasChanged();

            // 2. Ищем выбранную плавку в очереди
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].LandingId == uid)
                {
                    // Нашли плавку
                    meltNo = order[i].MeltNumber;
                    _logger.Info($"===== Начало перемещения вниз по очереди для плавки [{uid}] №{meltNo} =====");
                    found = true;
                    if (i < order.Count - 1)
                    {
                        // Это не самая нижняя плавка
                        if (order[i].WeightedIngots == 0 & order[i].Weighted == 0)
                        {
                            // Нет взвешенных заготовок
                            if (order[i + 1].WeightedIngots == 0 && order[i + 1].Weighted == 0)
                            {
                                // Следующая плавка не имеет взвешенных заготовок
                                LandingData tmp = order[i];
                                order[i] = order[i + 1];
                                order[i + 1] = tmp;
                                ordered = true;
                                break;
                            }

                            _logger.Error(
                                $"Следующая плавка [{order[i + 1].LandingId}] №{order[i + 1].MeltNumber} имеет взвешенные заготовки! Нельзя опустить вниз!");
                        }
                        else
                        {
                            _logger.Error(
                                $"Плавка [{uid}] №{order[i].MeltNumber} имеет взвешенные заготовки, нельзя опустить вниз!");
                        }
                    }
                    else
                    {
                        _logger.Error($"Плавка [{uid}] №{order[i].MeltNumber} самая нижняя, нельзя опустить вниз!");
                    }
                }
            }

            if (!found)
                _logger.Error($"Плавка с идентификатором {uid} не найдена в очереди!");
            
            if(ordered)
            {
                int newCnt;
                int oldCnt;

                // Пока количество удаленных плавок не будет равно количеству добавленных, или не более 5 раз
                int repeat = 1;
                do
                {
                    _logger.Info($">>>>> Попытка №{repeat}");
                    
                    // Очистить текущую очередь
                    _logger.Info($"Плавка [{uid}] => Начата очистка текущей очереди");
                    oldCnt = ClearCurrentOrder();
                    _logger.Info($"Плавка [{uid}] => Завершена очистка текущей очереди");

                    // Заполнить новую очередь
                    _logger.Info($"Плавка [{uid}] => Начато заполнение нового порядка очереди. Удалено плавок [{oldCnt}]");
                    newCnt = SetNewOrder(order);
                    _logger.Info($"Плавка [{uid}] => Завершено заполнение нового порядка очереди. Добавлено плавок [{newCnt}]");
                    repeat++;
                } while (oldCnt != newCnt && repeat < 6);
                
                if(repeat == 5)
                    _logger.Error($">>>>> Ошибка при перемещении плавки [{uid}] ${meltNo} вниз");
            }

            _movingButtonsState = true;
            _setLoading(false);
            ShowMessage(MessageType.Success, $"Плавка №{meltNo} перемещена вниз по очереди");
            _logger.Info($"===== Завершение перемещения вниз по очереди для плавки [{uid}] №{meltNo} =====");
            await Task.Delay(5000);
            HideMessage();
            StateHasChanged();
        }

        /// <summary>
        /// Переназначение заготовок внутри посада
        /// </summary>
        private void Remap()
        {
            int start = _remap.StartPos;
            int finish = _remap.EndPos;

            int landingId = _remap.Landing.LandingId; 
            string customer = _remap.Landing.Customer;
            string standard = _remap.Landing.Standart;
            
        }

        /// <summary>
        /// Очистить текущую очередь на посаде печи
        /// </summary>
        private int ClearCurrentOrder()
        {
            int result = 0;
            List<LandingData> order = GetLandingOrder(true);

            foreach (LandingData melt in order)
            {
                if (melt.WeightedIngots == 0 && melt.Weighted == 0)
                {
                    _logger.Info(
                        $"Начато удаление плавки [{melt.LandingId}] №{melt.MeltNumber} при очистке очереди");
                    int id = Db.Remove(melt.LandingId);
                    _logger.Warn($"Удалена плавка [{id}] №{melt.MeltNumber} при очистке очереди");
                    
                    // Находим идент плавки на тесте по иденту плавки на проде
                    int testUid = Db.GetTestLandingId(melt.LandingId);

                    _logger.Info($"Начато удаление плавки на тесте [{melt.LandingId}] №{melt.MeltNumber} при очистке очереди");
                    id = Db.Remove(testUid, true);
                    _logger.Warn($"Удалена плавка на тесте [{id}] №{melt.MeltNumber} при очистке очереди");

                    Task.Delay(TimeSpan.FromMilliseconds(500));
                    result++;
                }
            }

            // Обновление списка плавок после очистки
            _landed = GetLandingOrder(true);
            return result;
        }

        /// <summary>
        /// Установить новую очередь на посаде печи
        /// </summary>
        /// <param name="order">Плавка для постановки в очередь</param>
        private int SetNewOrder(List<LandingData> order)
        {
            int result = 0;
            for (int i = order.Count - 1; i >= 0; i--)
            {
                if (order[i].WeightedIngots == 0 && order[i].Weighted == 0)
                {
                    _logger.Info(
                        $"Начало добавления плавки на проде [{order[i].LandingId}] №{order[i].MeltNumber} при заполнении очереди");
                    int prodId = Db.CreateOvenLanding(order[i]);
                    _logger.Warn($"Добавлена плавка на проде [{prodId}] №{order[i].MeltNumber} при заполнении очереди");
                        
                    _logger.Info(
                        $"Начало добавления плавки на тесте [{order[i].LandingId}] №{order[i].MeltNumber} при заполнении очереди");
                    int testId = Db.CreateOvenLanding(order[i], true);
                    _logger.Warn($"Добавлена плавка на тесте [{testId}] №{order[i].MeltNumber} при заполнении очереди");
                    Task.Delay(TimeSpan.FromMilliseconds(500));
                        
                    _logger.Info($"Начата установка соответствия для плавки на проде [{prodId}] и на тесте [{testId}]");
                    bool rel = Db.SetRelation(prodId, testId.ToString());
                    if (!rel)
                    {
                        string message = $"Не удалось установить соответствие плавки между тестом [{testId}] и продом [{prodId}]";
                        _logger.Error(message);
                    }
                    else
                    {
                        string message = $"Установлено соответствие плавки между тестом [{testId}] и продом [{prodId}]";
                        _logger.Error(message);
                    }

                    result++;
                }
            }

            // Обновление списка плавок после очистки
            _landed = GetLandingOrder(true);
            return result;
        }

        private async Task EditLanding(int uid)
        {
            LandingData edit = new LandingData();
            LandingData orig = new LandingData();
           
            foreach (LandingData item in _landed)
            {
                if (item.LandingId == uid)
                {
                    edit.LandingId = item.LandingId; 
                    orig.LandingId = item.LandingId;
                    edit.MeltNumber = item.MeltNumber;
                    orig.MeltNumber = item.MeltNumber;
                    edit.IngotsCount = item.IngotsCount;
                    orig.IngotsCount = item.IngotsCount;
                    edit.IngotLength = item.IngotLength;
                    orig.IngotLength = item.IngotLength;
                    edit.SteelMark = item.SteelMark;
                    orig.SteelMark = item.SteelMark;
                    edit.IngotProfile = item.IngotProfile;
                    orig.IngotProfile = item.IngotProfile;
                    edit.WeightOne = item.WeightOne;
                    orig.WeightOne = item.WeightOne;
                    edit.WeightAll = item.WeightAll;
                    orig.WeightAll = item.WeightAll;
                    edit.Weighted = item.Weighted;
                    orig.Weighted = item.Weighted;
                    edit.ProductCode = item.ProductCode;
                    orig.ProductCode = item.ProductCode;
                    edit.Customer = item.Customer;
                    orig.Customer = item.Customer;
                    edit.Standart = item.Standart;
                    orig.Standart = item.Standart;
                    edit.Diameter = item.Diameter;
                    orig.Diameter = item.Diameter;
                    edit.Shift = item.Shift;
                    orig.Shift = item.Shift;
                    edit.IngotClass = item.IngotClass;
                    orig.IngotClass = item.IngotClass;
                    edit.ProductProfile = item.ProductProfile;
                    orig.ProductProfile = item.ProductProfile;
                    break;
                }
            }
            
            // Находим идент плавки на тесте по иденту плавки на проде
            int testUid = Db.GetTestLandingId(orig.LandingId);
            edit.LandingId = testUid;
            
            // List<LandingData> testMelts = /* await */ GetLandingOrder(true);
            // foreach (LandingData melt in testMelts)
            // {
            //     if (melt.MeltNumber == orig.MeltNumber && Math.Abs(melt.Diameter - orig.Diameter) < 0.1)
            //     {
            //         edit.LandingId = melt.LandingId;
            //     }
            // }
            
            _landingService.SetEditable(orig, edit);
            await JSRuntime.InvokeAsync<string>("openEditor", null);
        }

        /// <summary>
        /// Удалить посад по его идентификатору
        /// </summary>
        /// <param name="uid">Идентификатор посада</param>
        private async void Remove(int uid)
        {
            string meltNo = "";
            int oldCnt = 0;
            int weighted = 0;
            
            foreach (LandingData melt in _landed)
            {
                if (melt.LandingId == uid)
                {
                    meltNo = melt.MeltNumber;
                    oldCnt = melt.IngotsCount;
                    weighted = melt.WeightedIngots;
                    break;
                }
            }

            _logger.Info(
                $"===== Начато удаление плавки [{uid}] №{meltNo}, содержащей {oldCnt} заготовок из очереди =====");
            
            // Если количество взвешенных заготовок больше нуля, то в цикле удалить все невзвешенные заготовки
            if (weighted == 0)
            {
                // Нет взвешенных заготовок, удаляем посад целиком
                Db.Remove(uid);
                
                // Получим идент плавки на тесте по иденту плавки на проде
                int testUid = Db.GetTestLandingId(uid);
                Db.Remove(testUid, true);
                
                _logger.Warn($"Удалена плавка №{meltNo}, содержащей {oldCnt} заготовок из очереди");
            }
            else
            {
                // Есть взвешенные заготовки, удаляем только те, что ещё не взвешены
                for (int i = 0; i < oldCnt - weighted; i++)
                {
                    Db.DecLanding(uid);
                    
                    // Получим идент плавки на тесте по иденту плавки на проде
                    int testUid = Db.GetTestLandingId(uid);
                    Db.DecLanding(testUid, true);
                    
                    _logger.Warn($"[{i + 1}] Удалена заготовка из плавки №{meltNo}, всего заготовок [{oldCnt}], взвешено заготовок [{weighted}]");
                    await Task.Delay(500);
                }
            }

            _landed = GetLandingOrder();

            ShowMessage(MessageType.Success, $"Удалена из очереди плавка №{meltNo}");
            _logger.Info($"Удалена из очереди плавка [{uid}] №{meltNo}, содержащая {oldCnt} заготовок");
            _logger.Info(
                $"===== Завершено удаление плавки [{uid}] №{meltNo}, содержащей {oldCnt} заготовок из очереди =====");
            
            await Task.Delay(5000);
            StateHasChanged();
            
        }

        private void SetTimer(int seconds)
        {
            _timer = new Timer(seconds * 1000);
            _timer.Elapsed += UpdateData;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void UpdateData(Object source, ElapsedEventArgs e)
        {
            _landed = GetLandingOrder();
            _landingService.IngotsCount = DateTime.Now.Millisecond;
            _semaphoreColor = _semaphoreColor == "lightsteelblue" ? "#1861ac" : "lightsteelblue";
        }

        private async void UpdateMessage(object sender, PropertyChangedEventArgs args)
        {
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Получить корректное представление профиля диаметра
        /// </summary>
        /// <param name="profile">Профиль</param>
        /// <param name="diam">Диаметр</param>
        /// <returns>Корректная строка профиля</returns>
        private string _getDiameter(string profile, double diam)
        {
            string result;
            if (profile == "№")
            {
                result = profile + diam.ToString("F0");
            }
            else
            {
                string diamStr = diam.ToString("F2");
                result = diamStr[^1] == '0' ? profile + diam.ToString("F1") : profile + diam.ToString("F2");
            }

            return result;
        }

        /// <summary>
        /// Распределение ЕУ между посадами
        /// </summary>
        /// <param name="uid">Распределяемый посад</param>
        private void MeltRemap(int uid)
        {
            // Получение списка стандартов и заказчиков
            _standards = Db.GetGosts();
            _customers = Db.GetCustomers();
            
            List<PassportParam> passportParams = Db.GetPassportParams(uid);
            foreach (PassportParam param in passportParams)
            {
                switch (param.ParamId)
                {
                    case 10000001: // Номер плавки
                        _remap.Landing.MeltNumber = param.ValueString; break; 
                    case 10000002: // Марка стали
                        _remap.Landing.SteelMark = param.ValueString; break; 
                    case 10000003: // Сечение
                        _remap.Landing.IngotProfile = param.ValueString; break; 
                    case 10000004: // Количество заготовок
                        _remap.Landing.IngotsCount = int.Parse(param.ValueString); break; 
                    case 10000005: // Вес заготовок
                        _remap.Landing.WeightAll = int.Parse(param.ValueString); break; 
                    case 10000006: // Вес заготовки
                        _remap.Landing.WeightOne = int.Parse(param.ValueString); break; 
                    case 10000007: // Длина заготовки
                        _remap.Landing.IngotLength = int.Parse(param.ValueString); break; 
                    case 10000009: // Стандарт
                        _remap.Landing.Standart = param.ValueString; break; 
                    case 10000010: // Диаметр
                        _remap.Landing.Diameter = double.Parse(param.ValueString.Replace(".", ",")); break; 
                    case 10000011: // Заказчик
                        _remap.Landing.Customer = param.ValueString; break; 
                    case 10000012: // Смена
                        _remap.Landing.Shift = param.ValueString; break; 
                    case 10000013: // Класс
                        _remap.Landing.IngotClass = param.ValueString; break; 
                    case 10000015: // Код продукта
                        _remap.Landing.ProductCode = int.Parse(param.ValueString); break; 
                    case 10000018: // Профиль
                        _remap.Landing.ProductProfile = param.ValueString; break; 
                }
            }
            
            _landingId = uid;
        }
    }
}
