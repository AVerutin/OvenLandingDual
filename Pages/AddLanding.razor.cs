using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using NLog;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class AddLanding : IDisposable
    {
        private LandingData _landingData = new ();
        private readonly ProfileData _profileData = new ();
        private readonly SteelData _steelData = new ();
        private readonly GostData _gostData = new ();
        private readonly CustomerData _customerData = new ();
        private readonly ClassData _classData = new ();
        private List<string> _profiles = new ();
        private List<string> _steels = new ();
        private List<string> _gosts = new ();
        private List<string> _customers = new ();
        private List<string> _classes = new ();
        private readonly Shift _shift = new ();

        private Logger _logger;
        private readonly DbConnection _db = new ();
        private string _showWindowAddProfile = "none";
        private string _showWindowAddSteel = "none";
        private string _showWindowAddGost = "none";
        private string _showWindowAddCustomer = "none";
        private string _showWindowAddClass = "none";
        
        private bool _enabledPrecision = true;
        private string _profileType;

        private string ProfileType
        {
            get => _profileType;
            set
            {
                _profileType = value;
                
                _landingData.ProductProfile = value;
                _enabledPrecision = _landingData.ProductProfile != "№";
            }
        }

        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";

        protected override void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            Initialize();
        }

        private void Initialize()
        {
            UpdateDictionaries();
            _landingData = _db.GetState();
            int shift = _shift.GetShiftNumber(DateTime.Now);
            _landingData.Shift = shift.ToString();
            ProfileType = _landingData.ProductProfile;

            StateHasChanged();
        }

        public void Dispose()
        {
            _db.SaveState(_landingData);
        }

        /// <summary>
        /// Обновить данные из справочников
        /// </summary>
        private void UpdateDictionaries()
        {
            _profiles = _db.GetProfiles();
            _steels = _db.GetSteels();
            _gosts = _db.GetGosts();
            _customers = _db.GetCustomers();
            _classes = _db.GetClasses();
        }

        /// <summary>
        /// Добавить плавку в очередь на посад печи
        /// </summary>
        private async void AddNewLanding()
        {
            if(!string.IsNullOrEmpty(_landingData.MeltNumber))
            {
                _logger.Info($"===== Начало добавление плавки [{_landingData.LandingId}] №{_landingData.MeltNumber} в очередь на посаде");

                // Проверка на корректность заполнения сечения заготовки
                if (string.IsNullOrEmpty(_landingData.IngotProfile))
                {
                    _profiles = _db.GetProfiles();
                    if (_profiles.Count > 0)
                    {
                        _landingData.IngotProfile = _profiles[0];
                    }
                    else
                    {
                        ShowMessage(MessageType.Danger, "Не заполнено поле [Сечение заготовки]");
                        goto finish;
                    }
                }

                // Проверка на корректность заполнения марки стали
                if (string.IsNullOrEmpty(_landingData.SteelMark))
                {
                    _steels = _db.GetSteels();
                    if (_steels.Count > 0)
                    {
                        _landingData.SteelMark = _steels[0];
                    }
                    else
                    {
                        ShowMessage(MessageType.Danger, "Не заполнено поле [Марка стали]");
                        goto finish;
                    }
                }

                // Проверка на корректность заполнения заказчика
                if (string.IsNullOrEmpty(_landingData.Customer))
                {
                    _customers = _db.GetCustomers();
                    if (_customers.Count > 0)
                    {
                        _landingData.Customer = _customers[0];
                    }
                    else
                    {
                        ShowMessage(MessageType.Danger, "Не заполнено поле [Заказчик]");
                        goto finish;
                    }
                }

                // Проверка на корректность заполнения ГОСТа
                if (string.IsNullOrEmpty(_landingData.Standart))
                {
                    _gosts = _db.GetGosts();
                    if (_gosts.Count > 0)
                    {
                        _landingData.Standart = _gosts[0];
                    }
                    else
                    {
                        ShowMessage(MessageType.Danger, "Не заполнено поле [Стандарт]");
                        goto finish;
                    }
                }

                // Проверка на корректность заполнения класса
                if (string.IsNullOrEmpty(_landingData.IngotClass))
                {
                    _classes = _db.GetClasses();
                    if (_classes.Count > 0)
                    {
                        _landingData.IngotClass = _classes[0];
                    }
                    else
                    {
                        ShowMessage(MessageType.Danger, "Не заполнено поле [Класс]");
                        goto finish;
                    }
                }
                
                // Проверка на корректность заполнения количества заготовок
                if (_landingData.IngotsCount == 0)
                {
                    ShowMessage(MessageType.Danger, "Не заполнено поле [Количество заготовок]");
                    goto finish;
                }
                
                // Проверка на корректность заполнения длины заготовки
                if (_landingData.IngotLength == 0)
                {
                    ShowMessage(MessageType.Danger, "Не заполнено поле [Длина заготовки]");
                    goto finish;
                }
                
                // Проверка на корректность заполнения веса заготовки
                if (_landingData.WeightOne == 0)
                {
                    ShowMessage(MessageType.Danger, "Не заполнено поле [Вес заготовки]");
                    goto finish;
                }                
                
                // Проверка на корректность заполнения кода продукции
                if (_landingData.ProductCode == 0)
                {
                    ShowMessage(MessageType.Danger, "Не заполнено поле [Код продукции]");
                    goto finish;
                }
                
                // Проверка на корректность заполнения диаметра
                if ((int)_landingData.Diameter == 0)
                {
                    ShowMessage(MessageType.Danger, "Не заполнено поле [Диаметр]");
                    goto finish;
                }

                // Проверка значения в поле точности профиля
                if (string.IsNullOrEmpty(_landingData.DiameterPrecision))
                {
                    _landingData.DiameterPrecision = "1";
                }
                
                // Проверка на корректность заполнения номера бригады
                if (string.IsNullOrEmpty(_landingData.Shift))
                {
                    ShowMessage(MessageType.Danger, "Не заполнено поле [Бригада]");
                    goto finish;
                    // _landingData.Shift = "Не задано";
                }
                
                // Проверка на корректность заполнения профиля годной продукции
                if (string.IsNullOrEmpty(_landingData.ProductProfile))
                {
                    _landingData.ProductProfile = "№";
                }
                
                _landingData.WeightAll = _landingData.WeightOne * _landingData.IngotsCount;
                int uid = _db.CreateOvenLanding(_landingData);
                int uidTest = _db.CreateOvenLanding(_landingData, true);
                bool rel = _db.SetRelation(uid, uidTest.ToString());

                if (uid == -1)
                {
                    string message = $"Ошибка при добавлении плавки №{_landingData.MeltNumber} в базу данных";
                    _logger.Error(message);
                    ShowMessage(MessageType.Danger, message);
                }
                else
                {
                    string message = $"Добавлена плавка №{_landingData.MeltNumber} - UID = {uid}, количество заготовок в плавке: {_landingData.IngotsCount}";
                    _logger.Info("===== " + message + " =====");
                    ShowMessage(MessageType.Success, $"Добавлена плавка №{_landingData.MeltNumber}");
                }
                
                if (uidTest == -1)
                {
                    string message = $"Ошибка при добавлении плавки на тесте №{_landingData.MeltNumber} в базу данных";
                    _logger.Error(message);
                    ShowMessage(MessageType.Danger, message);
                }
                else
                {
                    string message = $"Добавлена плавка на тесте №{_landingData.MeltNumber} - UID = {uidTest}, количество заготовок в плавке: {_landingData.IngotsCount}";
                    _logger.Info("===== " + message + " =====");
                    ShowMessage(MessageType.Success, $"Добавлена плавка на тесте №{_landingData.MeltNumber}");
                }

                if (!rel)
                {
                    string message = $"Не удалось установить соответствие плавки между тестом [{uid}] и продом [{uidTest}]";
                    _logger.Error(message);
                }
                else
                {
                    string message = $"Установлено соответствие плавки между тестом [{uid}] и продом [{uidTest}]";
                    _logger.Info(message);
                }
                
                _landingData.MeltNumber = "";
                _landingData.IngotsCount = 0;
                _db.SaveState(_landingData);
                StateHasChanged();
            }
            else
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Номер плавки]");
            }
            
            finish:
            await Task.Delay(TimeSpan.FromSeconds(5));
            HideMessage();
            StateHasChanged();
        }

        /// <summary>
        /// Обработка выбора типа прокатываемого профиля
        /// </summary>
        /// <param name="e"></param>
        private void ChangeProfile(ChangeEventArgs e)
        {
            _landingData.ProductProfile = string.IsNullOrEmpty(e.Value?.ToString()) ? "" : e.Value.ToString();
            if (_landingData.ProductProfile == "№")
                _enabledPrecision = false;
            else
                _enabledPrecision = true;
        }

        // Profile
        private void ShowProfile()
        {
            _profileData.ProfileName = "";
            _showWindowAddClass = "none";
            _showWindowAddCustomer = "none";
            _showWindowAddGost = "none";
            _showWindowAddSteel = "none";
            _showWindowAddProfile = "block";
        }

        private void AddProfile()
        {
            string profileName = _profileData.ProfileName ?? "";
            int res = _db.AddProfile(profileName);

            switch (res)
            {
                case 0:
                {
                    // Добавили профиль заготовки
                    _profiles = _db.GetProfiles();
                    _logger.Info($"Добавлено сечение заготовки [{profileName}]");
                    StateHasChanged();
                    break;
                }
                case -1:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить сечение заготовки [{profileName}] на проде");
                    StateHasChanged();
                    break;
                }
                case -10:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить сечение заготовки [{profileName}] на тесте");
                    StateHasChanged();
                    break;
                }
                case -11:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить сечение заготовки [{profileName}] на тесте и на проде");
                    StateHasChanged();
                    break;
                }
            }

            _showWindowAddProfile = "none";
        }

        // Steel
        private void ShowSteel()
        {
            // Прячем все остальные формы
            _steelData.SteelName = "";
            _showWindowAddClass = "none";
            _showWindowAddCustomer = "none";
            _showWindowAddGost = "none";
            _showWindowAddProfile = "none";
            _showWindowAddSteel = "block";
        }
        
        private void AddSteel()
        {
            string steelName = _steelData.SteelName ?? "";
            int res = _db.AddSteel(steelName);

            switch (res)
            {
                case 0:
                {
                    // Добавили марку стали
                    _steels = _db.GetSteels();
                    _logger.Info($"Добавлена марка стали [{steelName}]");
                    StateHasChanged();
                    break;
                }
                case -1:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить марку стали [{steelName}] на проде");
                    StateHasChanged();
                    break;
                }
                case -10:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить марку стали [{steelName}] на тесте");
                    StateHasChanged();
                    break;
                }
                case -11:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить марку стали [{steelName}] на тесте и на проде");
                    StateHasChanged();
                    break;
                }
            }

            _showWindowAddSteel = "none";
        }
        
        // GOST
        private void ShowGost()
        {
            _gostData.GostName = "";
            _showWindowAddClass = "none";
            _showWindowAddCustomer = "none";
            _showWindowAddProfile = "none";
            _showWindowAddSteel = "none";
            _showWindowAddGost = "block";
        }

        private void AddGost()
        {
            string gostName = _gostData.GostName ?? "";
            int res = _db.AddGost(gostName);

            switch (res)
            {
                case 0:
                {
                    // Добавили профиль заготовки
                    _gosts = _db.GetGosts();
                    _logger.Info($"Добавлен ГОСТ [{gostName}]");
                    StateHasChanged();
                    break;
                }
                case -1:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить ГОСТ [{gostName}] на проде");
                    StateHasChanged();
                    break;
                }
                case -10:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить ГОСТ [{gostName}] на тесте");
                    StateHasChanged();
                    break;
                }
                case -11:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить ГОСТ [{gostName}] на тесте и на проде");
                    StateHasChanged();
                    break;
                }
            }

            _showWindowAddGost = "none";
        }
        
        // Customer
        private void ShowCustomer()
        {
            _customerData.Customer = "";
            _showWindowAddClass = "none";
            _showWindowAddGost = "none";
            _showWindowAddProfile = "none";
            _showWindowAddSteel = "none";
            _showWindowAddCustomer = "block";

        }

        private void AddCustomer()
        {
            string customerName = _customerData.Customer ?? "";
            int res = _db.AddCustomer(customerName);

            switch (res)
            {
                case 0:
                {
                    // Добавили профиль заготовки
                    _customers = _db.GetCustomers();
                    _logger.Info($"Добавлен заказчик [{customerName}]");
                    StateHasChanged();
                    break;
                }
                case -1:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить заказчика [{customerName}] на проде");
                    StateHasChanged();
                    break;
                }
                case -10:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить заказчика [{customerName}] на тесте");
                    StateHasChanged();
                    break;
                }
                case -11:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить заказчика [{customerName}] на тесте и на проде");
                    StateHasChanged();
                    break;
                }
            }
            
            _showWindowAddCustomer = "none";
        }
        
        // Class
        private void ShowClass()
        {
            _classData.Class = "";
            _showWindowAddGost = "none";
            _showWindowAddProfile = "none";
            _showWindowAddSteel = "none";
            _showWindowAddCustomer = "none";
            _showWindowAddClass = "block";
        }

        private void AddClass()
        {
            string className = _classData.Class ?? "";
            int res = _db.AddClass(className);

            switch (res)
            {
                case 0:
                {
                    // Добавили профиль заготовки
                    _classes = _db.GetClasses();
                    _logger.Info($"Добавлен класс [{className}]");
                    StateHasChanged();
                    break;
                }
                case -1:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить класс [{className}] на проде");
                    StateHasChanged();
                    break;
                }
                case -10:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить класс [{className}] на тесте");
                    StateHasChanged();
                    break;
                }
                case -11:
                {
                    // Не добавили профиль заготовки
                    _logger.Error($"Не удалось добавить класс [{className}] на тесте и на проде");
                    StateHasChanged();
                    break;
                }
            }

            _showWindowAddClass = "none";
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
    }
}