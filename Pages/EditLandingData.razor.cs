using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class EditLandingData : IDisposable
    {
        private LandingData _editData = new ();
        private LandingData _origData = new ();
        private List<string> _profiles = new ();
        private List<string> _steels = new ();
        private List<string> _gosts = new ();
        private List<string> _customers = new ();
        private List<string> _classes = new ();
        private readonly Shift _shift = new ();
        
        // private IConfigurationRoot _config;
        private Logger _logger;
        private readonly DbConnection _db = new ();
        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";
        
        private bool _enabledPrecision = true;
        private string _profileType;

        private string ProfileType
        {
            get => _profileType;
            set
            {
                _profileType = value;
                
                _editData.ProductProfile = value;
                _enabledPrecision = _editData.ProductProfile != "№"; 
            }
        }
        
        protected override void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            Initialize();
        }

        public void Dispose()
        {
            _logger.Info($"Закрыта панель редактирования плавки №{_origData.MeltNumber} [{_origData.LandingId}]");
        }

        private void Initialize()
        {
            _editData = _landingService.EditMode ? _landingService.GetEditable() : new LandingData();
            _origData = _landingService.EditMode ? _landingService.GetOriginal() : new LandingData();
            _logger.Info($"Открыта панель редактирования плавки №{_origData.MeltNumber} [{_origData.LandingId}]");
            ProfileType = _origData.ProductProfile;
            _enabledPrecision = true;

            _profiles = _db.GetProfiles();
            _steels = _db.GetSteels();
            _gosts = _db.GetGosts();
            _customers = _db.GetCustomers();
            _classes = _db.GetClasses();

            StateHasChanged();
        }

        private void ShowMessage(MessageType type, string message)
        {
            _message = message ?? "";
            switch (type)
            {
                case MessageType.Primary: _messageClass = "alert alert-primary"; break;
                case MessageType.Secondary: _messageClass = "alert alert-secondary"; break;
                case MessageType.Success: _messageClass = "alert alert-success"; break;
                case MessageType.Danger: _messageClass = "alert alert-danger"; break;
                case MessageType.Warning: _messageClass = "alert alert-warning"; break;
                case MessageType.Info: _messageClass = "alert alert-info"; break;
                case MessageType.Light: _messageClass = "alert alert-light"; break;
                case MessageType.Dark: _messageClass = "alert alert-dark"; break;
            }

            _messageVisible = "block";
            StateHasChanged();
        }

        private void HideMessage()
        {
            _message = "";
            _messageVisible = "none";
            StateHasChanged();
        }

        private async void EditLanding()
        {
            // Проверка на корректность заполнения сечения заготовки
            if (string.IsNullOrEmpty(_editData.IngotProfile))
            {
                _profiles = _db.GetProfiles();
                _editData.IngotProfile = _profiles.Count > 0 ? _profiles[0] : "0";
            }

            // Проверка на корректность заполнения марки стали
            if (string.IsNullOrEmpty(_editData.SteelMark))
            {
                _steels = _db.GetSteels();
                _editData.SteelMark = _steels.Count > 0 ? _steels[0] : "Не задано";
            }

            // Проверка на корректность заполнения заказчика
            if (string.IsNullOrEmpty(_editData.Customer))
            {
                _customers = _db.GetCustomers();
                _editData.Customer = _customers.Count > 0 ? _customers[0] : "Не задано";
            }

            // Проверка на корректность заполнения ГОСТа
            if (string.IsNullOrEmpty(_editData.Standart))
            {
                _gosts = _db.GetGosts();
                _editData.Standart = _gosts.Count > 0 ? _gosts[0] : "Не задано";
            }

            // Проверка на корректность заполнения класса
            if (string.IsNullOrEmpty(_editData.IngotClass))
            {
                _classes = _db.GetClasses();
                _editData.IngotClass = _classes.Count > 0 ? _classes[0] : "Не задано";
            }

            // Проверка на корректность заполнения длины заготовки
            if (_editData.IngotLength == 0)
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Длина заготовки]");
                goto finish;
            }

            // Проверка на корректность заполнения веса заготовки
            if (_editData.WeightOne == 0)
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Вес заготовки]");
                goto finish;
            }

            // Проверка на корректность заполнения кода продукции
            if (_editData.ProductCode == 0)
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Код продукции]");
                goto finish;
            }

            // Проверка на корректность заполнения диаметра
            if ((int) _editData.Diameter == 0)
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Диаметр]");
                goto finish;
            }

            // Проверка значения в поле точности профиля
            if (string.IsNullOrEmpty(_editData.DiameterPrecision))
            {
                _editData.DiameterPrecision = "1";
            }

            // Проверка на корректность заполнения номера бригады
            if (string.IsNullOrEmpty(_editData.Shift))
            {
                _editData.Shift = _shift.GetCurrentShiftNumber().ToString();
            }

            // Проверка на корректность заполнения профиля годной продукции
            if (string.IsNullOrEmpty(_editData.ProductProfile))
            {
                _editData.ProductProfile = "№";
            }

            _logger.Info($"===== Начато обновление данных плавки {_origData.MeltNumber} =====");
            _editData.WeightAll = _editData.WeightOne * _editData.IngotsCount;

            // Вноси мизменения в параметры плавки на проде
            uint res = _db.EditMelt(_origData, _editData);
            if (res > 0)
            {
                string param;
                
                // Флаг изменения марки стали
                if ( (res & 0b_0000_0000_0000_0001) > 0) 
                {
                    param = "Марка стали";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения сечения заготовки
                if ( (res & 0b_0000_0000_0000_0010) > 0) 
                {
                    param = "Сечение заготовки";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения профиля годной продукции
                if ( (res & 0b_0000_0000_0000_0100) > 0)
                {
                    param = "Профиль";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения длины заготовки
                if ( (res & 0b_0000_0000_0000_1000) > 0)
                {
                    param = "Длина заготовки";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения стандарта
                if ( (res & 0b_0000_0000_0001_0000) > 0)
                {
                    param = "Стандарт";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения диаметра
                if ( (res & 0b_0000_0000_0010_0000) > 0)
                {
                    param = "Диаметр";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения заказчика
                if ( (res & 0b_0000_0000_0100_0000) > 0)
                {
                    param = "Заказчик";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения бригады
                if ( (res & 0b_0000_0000_1000_0000) > 0)
                {
                    param = "Бригада";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения класса
                if ( (res & 0b_0000_0001_0000_0000) > 0)
                {
                    param = "Класс продукции";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения номера плавки
                if ( (res & 0b_0000_0010_0000_0000) > 0)
                {
                    param = "Номер плавки";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения кода продукции
                if ( (res & 0b_0000_0100_0000_0000) > 0)
                {
                    param = "Код продукции";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения веса одной заготовки
                if ( (res & 0b_0000_1000_0000_0000) > 0)
                {
                    param = "Вес заготовки";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения веса всех заготовок
                if ( (res & 0b_0001_0000_0000_0000) > 0)
                {
                    param = "Вес заготовок";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на проде {_origData.LandingId} возникла ошибка");
                }
                
                // Нет новых данных для изменения плавки
                if ( (res & 0b_0010_0000_0000_0000) > 0)
                {
                    _logger.Error(
                        $"При изменении параметров плавки на проде {_origData.LandingId} возникла ошибка => Нет новых данных для изменения плавки");
                }
                
                // Идентификатор плавки равен нулю
                if ( (res & 0b_0100_0000_0000_0000) > 0)
                {
                    _logger.Error(
                        $"При изменении параметров плавки на проде {_origData.LandingId} возникла ошибка => Идентификатор плавки равен нулю");
                }
            }
            
            // Вносим изменения в параметры павки на тесте
            res = _db.EditMelt(_origData, _editData, true);
            if (res > 0)
            {
                string param;
                
                // Флаг изменения марки стали
                if ( (res & 0b_0000_0000_0000_0001) > 0) 
                {
                    param = "Марка стали";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения сечения заготовки
                if ( (res & 0b_0000_0000_0000_0010) > 0) 
                {
                    param = "Сечение заготовки";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения профиля годной продукции
                if ( (res & 0b_0000_0000_0000_0100) > 0)
                {
                    param = "Профиль";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения длины заготовки
                if ( (res & 0b_0000_0000_0000_1000) > 0)
                {
                    param = "Длина заготовки";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения стандарта
                if ( (res & 0b_0000_0000_0001_0000) > 0)
                {
                    param = "Стандарт";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения диаметра
                if ( (res & 0b_0000_0000_0010_0000) > 0)
                {
                    param = "Диаметр";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения заказчика
                if ( (res & 0b_0000_0000_0100_0000) > 0)
                {
                    param = "Заказчик";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения бригады
                if ( (res & 0b_0000_0000_1000_0000) > 0)
                {
                    param = "Бригада";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения класса
                if ( (res & 0b_0000_0001_0000_0000) > 0)
                {
                    param = "Класс продукции";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения номера плавки
                if ( (res & 0b_0000_0010_0000_0000) > 0)
                {
                    param = "Номер плавки";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения кода продукции
                if ( (res & 0b_0000_0100_0000_0000) > 0)
                {
                    param = "Код продукции";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения веса одной заготовки
                if ( (res & 0b_0000_1000_0000_0000) > 0)
                {
                    param = "Вес заготовки";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Флаг изменения веса всех заготовок
                if ( (res & 0b_0001_0000_0000_0000) > 0)
                {
                    param = "Вес заготовок";
                    _logger.Error(
                        $"При изменении параметра '{param}' плавки на тесте {_origData.LandingId} возникла ошибка");
                }
                
                // Нет новых данных для изменения плавки
                if ( (res & 0b_0010_0000_0000_0000) > 0)
                {
                    _logger.Error(
                        $"При изменении параметров плавки на тесте {_origData.LandingId} возникла ошибка => Нет новых данных для изменения плавки");
                }
                
                // Идентификатор плавки равен нулю
                if ( (res & 0b_0100_0000_0000_0000) > 0)
                {
                    _logger.Error(
                        $"При изменении параметров плавки на тесте {_origData.LandingId} возникла ошибка => Идентификатор плавки равен нулю");
                }
            }

            _logger.Info($"===== Завершено обновление данных плавки {_origData.MeltNumber} =====");
            _landingService.ClearEditable();
            await JSRuntime.InvokeAsync<string>("openQuery", null);
            
            finish:
            await Task.Delay(TimeSpan.FromSeconds(5));
            HideMessage();
            StateHasChanged();
        }
    }
}