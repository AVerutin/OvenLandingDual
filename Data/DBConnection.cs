using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using NLog;
using Npgsql;
using OvenLanding.Properties;

namespace OvenLanding.Data
{
    public class DbConnection
    {
        private readonly string _connectionString;
        private readonly string _connectionStringTest;
        private readonly Logger _logger;
        private readonly DbQueries _dbQueries;
        private readonly int _timeOutP;
        private readonly int _timeOutT;

        /// <summary>
        /// Конструктор создания подключения к базе данных
        /// </summary>
        public DbConnection()
        {
            // Читаем параметры подключения к СУБД PostgreSQL
            _logger = LogManager.GetCurrentClassLogger();
            _dbQueries = new DbQueries();
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            
            // Настройки для подключения к базе данных на проде
            string host = config.GetSection("DBConnection:Host").Value;
            int port = int.Parse(config.GetSection("DBConnection:Port").Value);
            string database = config.GetSection("DBConnection:Database").Value;
            string user = config.GetSection("DBConnection:UserName").Value;
            string dbPwd = config.GetSection("DBConnection:DbPwd").Value;
            _timeOutP = int.Parse(config.GetSection("DBConnection:Timeout").Value);
            string sslMode = config.GetSection("DBConnection:SslMode").Value;
            string trustServerCert = config.GetSection("DBConnection:TrustServerCertificate").Value;
            
            // Настройки для подключения к базе данных на тесте
            string hostT = config.GetSection("DBTest:Host").Value;
            int portT = int.Parse(config.GetSection("DBTest:Port").Value);
            string databaseT = config.GetSection("DBTest:Database").Value;
            string userT = config.GetSection("DBTest:UserName").Value;
            string dbPwdT = config.GetSection("DBTest:DbPwd").Value;
            _timeOutT = int.Parse(config.GetSection("DBTest:Timeout").Value);
            string sslModeT = config.GetSection("DBTest:SslMode").Value;
            string trustServerCertT = config.GetSection("DBTest:TrustServerCertificate").Value;
            
            // Строка подключения для базы данных на проде
            _connectionString =
                $"Server={host};Username={user};Database={database};Port={port};Password={dbPwd};" +
                $"SSL Mode={sslMode};Trust Server Certificate={trustServerCert}";
            
            // Строка подключения для базы данных на тесте
            _connectionStringTest =
                $"Server={hostT};Username={userT};Database={databaseT};Port={portT};Password={dbPwdT};" +
                $"SSL Mode={sslModeT};Trust Server Certificate={trustServerCertT}";
        }

        public int DbInit()
        {
            // Создание справочника профилей арматуры
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.profiles (" +
                           "id serial not null constraint profiles_pk primary key, " +
                           "profile varchar not null); " +
                           "comment on table public.profiles is 'Справочник видов профилей заготовки'; " +
                           "alter table public.profiles owner to mts;";
            int res = WriteData(query);
            if (res == -1)
            {
                _logger.Error("Ошибка при создании справочника профилей арматуры");
            }
            
            // Создание справочника марок стали
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.steels (" +
                    "id serial not null constraint steels_pk primary key, " +
                    "steel varchar not null); " +
                    "comment on table public.steels is 'Справочник марок стали'; " +
                    "alter table public.steels owner to mts;";
            
            res = WriteData(query);
            if (res == -1)
            {
                _logger.Error("Ошибка при создании справочника профилей арматуры");
            }
            
            // Создание справочника ГОСТов
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.gosts (" +
                    "id serial not null constraint gosts_pk primary key, " +
                    "gost varchar not null); " +
                    "comment on table public.gosts is 'Справочник ГОСТов'; " +
                    "alter table public.gosts owner to mts;";
            
            res = WriteData(query);
            if (res == -1)
            {
                _logger.Error("Ошибка при создании справочника стандартов");
            }
            
            // Создание справочника заказчиков
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.customers (" +
                    "id serial not null constraint customers_pk primary key, " +
                    "customer varchar not null); " +
                    "comment on table public.customers is 'Справочник заказчиков'; " +
                    "alter table public.customers owner to mts;";
            
            res = WriteData(query);
            if (res == -1)
            {
                _logger.Error("Ошибка при создании справочника заказчиков");
            }
            
            // Создание справочника классов
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.classes (" +
                    "id serial not null constraint classes_pk primary key, " +
                    "class varchar not null); " +
                    "comment on table public.classes is 'Справочник классов'; " +
                    "alter table public.classes owner to mts;";
            
            res = WriteData(query);
            if (res == -1)
            {
                _logger.Error("Ошибка при создании справочника классов");
            }
            
            // Создание таблицы заготовок на посаде печи
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.oven_landing (" +
                    "id serial not null, " +
                    "melt_number varchar(15), " +
                    "ingots_count numeric, " +
                    "ingot_length numeric, " +
                    "steel_mark varchar(25), " +
                    "ingot_profile varchar(10), " +
                    "ingot_weight numeric, " +
                    "production_code numeric, " +
                    "customer varchar(150), " +
                    "standart varchar(150), " +
                    "diameter numeric, " +
                    "shift varchar(15), " +
                    "class varchar(50), " +
                    "specification varchar(50), " +
                    "lot numeric, " +
                    "constraint oven_landing_pk primary key (id)); " +
                    "comment on table public.oven_landing is 'Сохранение данных полей формы ввода'; " +
                    "alter table public.oven_landing owner to mts;";
            
            res = WriteData(query);
            if (res == -1)
            {
                _logger.Error("Ошибка при создании таблицы [OvenLanding]");
            }
            
            // Проверяем, есть ли в таблице public.oven_landing записи
            if (GetLastId("oven_landing") == 0)
            {
                query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += "insert into public.oven_landing (melt_number, ingots_count, ingot_length, steel_mark, " +
                        "ingot_profile, ingot_weight, production_code, customer, standart, diameter, shift, class) VALUES (" +
                        "'', 0, 0, '', '', 0, 0, '', '', 0, '', '');";
                res = WriteData(query);
                if (res == -1)
                {
                    _logger.Error("Ошибка при добавлении записи в таблицу [OvenLanding]");
                }
            }
            
            return res;
        }
        
        /// <summary>
        /// Изменение параметров плавки
        /// </summary>
        /// <param name="oldMelt">Изменяемая плавка</param>
        /// <param name="newMelt">Измененная плавка</param>
        /// <param name="testDb">На тестовой безе, или на проде</param>
        /// <returns>Результат выполнения операции</returns>
        public uint EditMelt(LandingData oldMelt, LandingData newMelt, bool testDb=false)
        {
            uint ret = 0b_0000_0000_0000_0000;
            string dbType = testDb ? "TEST" : "PROD";
            int landingId = testDb ? newMelt.LandingId : oldMelt.LandingId;
            
            if (newMelt != null && landingId > 0)
            {
                int res;
                if (oldMelt.SteelMark != newMelt.SteelMark)
                {
                    res = ChangeParam(landingId, LandingParam.SteelMark, newMelt.SteelMark, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении марки стали с [{oldMelt.Shift}] на [{newMelt.SteelMark}] " +
                            $"для плавки ID={landingId} на [{newMelt.SteelMark}]");
                        ret |= 0b_0000_0000_0000_0001; // Флаг изменения марки стали
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменена марка стали с [{oldMelt.SteelMark}] на [{newMelt.SteelMark}]");
                    }
                }

                if (oldMelt.IngotProfile != newMelt.IngotProfile)
                {
                    res = ChangeParam(landingId, LandingParam.IngotProfile, newMelt.IngotProfile, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении сечения заготовки с [{oldMelt.IngotProfile}] на [{newMelt.IngotProfile}]  " +
                            $"для плавки ID={landingId} на [{newMelt.IngotProfile}]");
                        ret |= 0b_0000_0000_0000_0010; // Флаг изменения сечения заготовки
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменено сечения заготовки с [{oldMelt.IngotProfile}] на [{newMelt.IngotProfile}]");
                    }
                }

                if (oldMelt.ProductProfile != newMelt.ProductProfile)
                {
                    oldMelt.Diameter = 0;
                    res = ChangeParam(landingId, LandingParam.ProductProfile, newMelt.ProductProfile, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении профиля годной продукции с [{oldMelt.ProductProfile}] на [{newMelt.ProductProfile}] " +
                            $"для плавки ID={landingId} на [{newMelt.ProductProfile}]");
                        ret |= 0b_0000_0000_0000_0100; // Флаг изменения профиля годной продукции
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен профиль годной продукции с [{oldMelt.ProductProfile}] на [{newMelt.ProductProfile}]");
                    }
                }

                if (oldMelt.IngotLength != newMelt.IngotLength)
                {
                    res = ChangeParam(landingId, LandingParam.IngotLength, newMelt.IngotLength.ToString(), testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении длины заготовки с [{oldMelt.IngotLength}] на [{newMelt.IngotLength}] " +
                            $"для плавки ID={landingId} на [{newMelt.IngotLength}]");
                        ret |= 0b_0000_0000_0000_1000; // Флаг изменения длины заготовки
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменена длины заготовки с [{oldMelt.IngotLength}] на [{newMelt.IngotLength}]");
                    }
                }

                if (oldMelt.Standart != newMelt.Standart)
                {
                    res = ChangeParam(landingId, LandingParam.Standart, newMelt.Standart, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении стандарта с [{oldMelt.Standart}] на [{newMelt.Standart}] " +
                            $"для плавки ID={landingId} на [{newMelt.Standart}]");
                        ret |= 0b_0000_0000_0001_0000; // Флаг изменения стандарта
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен стандарт с [{oldMelt.Standart}] на [{newMelt.Standart}]");
                    }
                }
                
                if (Math.Abs(newMelt.Diameter - oldMelt.Diameter) >= 0.01)
                {
                    string diam = newMelt.Diameter.ToString(CultureInfo.CurrentCulture).Replace(",", ".");
            
                    //Если добавляем арматуру, то диаметр - целое число
                    if (newMelt.ProductProfile == "№")
                        diam = Math.Ceiling(newMelt.Diameter).ToString("F0").Replace(",", ".");
                    else
                    {
                        if (newMelt.DiameterPrecision == "1")
                            diam = newMelt.Diameter.ToString("F1").Replace(",", ".");
                        else if (newMelt.DiameterPrecision == "2")
                            diam = newMelt.Diameter.ToString("F2").Replace(",", ".");
                    }
            
                    res = ChangeParam(landingId, LandingParam.Diameter, diam, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении диаметра с [{oldMelt.Diameter}] на [{newMelt.Diameter}] " +
                            $"для плавки ID={landingId} на [{newMelt.Diameter}]");
                        ret |= 0b_0000_0000_0010_0000; // Флаг изменения диаметра
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен диаметра с [{oldMelt.Diameter}] на [{newMelt.Diameter}]");
                    }
                }

                if (oldMelt.Customer != newMelt.Customer)
                {
                    res = ChangeParam(landingId, LandingParam.Customer, newMelt.Customer, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении заказчика с [{oldMelt.Customer}] на [{newMelt.Customer}] " +
                            $"для плавки ID={landingId} на [{newMelt.Customer}]");
                        ret |= 0b_0000_0000_0100_0000; // Флаг изменения заказчика
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен заказчик с [{oldMelt.Customer}] на [{newMelt.Customer}]");
                    }
                }

                if (oldMelt.Shift != newMelt.Shift)
                {
                    res = ChangeParam(landingId, LandingParam.Shift, newMelt.Shift, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении бригады с [{oldMelt.Shift}] на [{newMelt.Shift}] " +
                            $"для плавки ID={landingId} на [{newMelt.Shift}]");
                        ret |= 0b_0000_0000_1000_0000; // Флаг изменения бригады
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменена бригада с [{oldMelt.Shift}] на [{newMelt.Shift}]");
                    }
                }

                if (oldMelt.IngotClass != newMelt.IngotClass)
                {
                    res = ChangeParam(landingId, LandingParam.Class, newMelt.IngotClass, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении класса с [{oldMelt.IngotClass}] на [{newMelt.IngotClass}] " +
                            $"для плавки ID={landingId} на [{newMelt.IngotClass}]");
                        ret |= 0b_0000_0001_0000_0000; // Флаг изменения класса
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен класс с [{oldMelt.IngotClass}] на [{newMelt.IngotClass}]");
                    }
                }

                if (oldMelt.MeltNumber != newMelt.MeltNumber)
                {
                    res = ChangeParam(landingId, LandingParam.MeltNumber, newMelt.MeltNumber, testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении номера плавки с [{oldMelt.MeltNumber}] на [{newMelt.MeltNumber}] " +
                            $"для плавки ID={landingId} на [{newMelt.MeltNumber}]");
                        ret |= 0b_0000_0010_0000_0000; // Флаг изменения номера плавки
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен номер плавки с [{oldMelt.MeltNumber}] на [{newMelt.MeltNumber}]");
                    }
                }

                if (oldMelt.ProductCode != newMelt.ProductCode)
                {
                    res = ChangeParam(landingId, LandingParam.ProductCode, newMelt.ProductCode.ToString(), testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении кода продукции с [{oldMelt.ProductCode}] на [{newMelt.ProductCode}] " +
                            $"для плавки ID={landingId} на [{newMelt.ProductCode}]");
                        ret |= 0b_0000_0100_0000_0000; // Флаг изменения кода продукции
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен код продукции с [{oldMelt.ProductCode}] на [{newMelt.ProductCode}]");
                    }
                }
                
                if (oldMelt.WeightOne!=newMelt.WeightOne)
                {
                    res = ChangeParam(landingId, LandingParam.WeightOne, newMelt.WeightOne.ToString(), testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении веса одной заготовки с [{oldMelt.WeightOne}] на [{newMelt.WeightOne}] " +
                            $"для плавки ID={landingId} на [{newMelt.WeightOne}]");
                        ret |= 0b_0000_1000_0000_0000; // Флаг изменения веса одной заготовки
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен вес одной заготовки с [{oldMelt.WeightOne}] на [{newMelt.WeightOne}]");
                    }

                    int weightAll = newMelt.IngotsCount * newMelt.WeightOne;
                    res = ChangeParam(landingId, LandingParam.WeightAll, weightAll.ToString(), testDb);
                    if (res == -1)
                    {
                        _logger.Error(
                            $"[{dbType}] Ошибка при изменении веса всех заготовок с [{oldMelt.WeightAll}] на [{weightAll}] " +
                            $"для плавки ID={landingId} на [{weightAll}]");
                        ret |= 0b_0001_0000_0000_0000; // Флаг изменения веса всех заготовок
                    }
                    else
                    {
                        _logger.Info($"Для плавки ID={landingId} изменен вес всех заготовок с [{oldMelt.WeightAll}] на [{weightAll}]");
                    }
                }
            }
            else
            {
                if (newMelt == null)
                {
                    _logger.Error("Нет новых данных для изменения плавки!");
                    ret |= 0b_0010_0000_0000_0000; // Нет новых данных для изменения плавки
                }

                if (landingId == 0)
                {
                    _logger.Error("Идентификатор плавки равен нулю!");
                    ret |= 0b_0100_0000_0000_0000; // Идентификатор плавки равен нулю
                }
            }

            return ret;
        }

        private int ChangeParam(int melt, LandingParam param, string value, bool test)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"call public.p_set_param({melt}, {(int) param}, '{value}'); ";

            return WriteData(query);
        }

        /// <summary>
        /// Получить идентификатор последней вставленной строки в таблицу
        /// </summary>
        /// <param name="tableName">Имя таблицы</param>
        /// <returns>Идентификатор последней вставленной строки</returns>
        private int GetLastId(string tableName)
        {
            int lastId = 0;
            string query = $"set session statement_timeout to '{_timeOutP}ms'; ";
            query += $"select max(id) from public.{tableName};";
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                lastId = int.Parse(dataTable.Rows[0]["max"].ToString() ?? "0");
            }

            return lastId;
        }

        /// <summary>
        /// Сохранить текущее состояние полей формы ввода
        /// </summary>
        public void SaveState(LandingData state)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            int lastId = GetLastId("oven_landing");
            if (lastId == 0)
            {
                query += "insert into public.oven_landing (melt_number, ingots_count, ingot_length, steel_mark, " +
                        "ingot_profile, ingot_weight, production_code, customer, standart, diameter, shift, class, product_profile, specification, lot, precision) VALUES (" +
                        "'{0}', {1}, {2}, '{3}', '{4}', {5}, {6}, '{7}', '{8}', {9}, '{10}', '{11}', '{12}', '{13}', {14}, '{15}');";
            }
            else
            {
                query += "update public.oven_landing set melt_number='{0}', ingots_count={1}, ingot_length={2}, steel_mark='{3}', " +
                        "ingot_profile='{4}', ingot_weight={5}, production_code={6}, customer='{7}', standart='{8}', diameter={9}, " +
                        "shift='{10}', class='{11}', product_profile='{12}', specification='{13}', lot={14}, precision='{15}' where id={16};";
            }
            
            string diam = state.Diameter.ToString("F1").Replace(",", ".");
            query = string.Format(query, state.MeltNumber, state.IngotsCount, state.IngotLength, state.SteelMark,
                state.IngotProfile, state.WeightOne, state.ProductCode, state.Customer, state.Standart, diam,
                state.Shift, state.IngotClass, state.ProductProfile, state.Specification, state.Lot, state.DiameterPrecision, lastId);

            int res = WriteData(query);
            if (res == -1)
            {
                _logger.Error("Ошибка при сохранении состояния полей формы ввода");
            }
        }

        /// <summary>
        /// Получить сохраненное состояние полей формы ввода
        /// </summary>
        /// <returns></returns>
        public LandingData GetState()
        {
            int lastId = GetLastId("oven_landing");
            LandingData result = new LandingData();
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.oven_landing where id = {lastId}; ";
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    try
                    {
                        result.MeltNumber = dataTable.Rows[i][1].ToString();
                        result.IngotsCount = int.Parse(dataTable.Rows[i][2].ToString() ?? "0");
                        result.IngotLength = int.Parse(dataTable.Rows[i][3].ToString() ?? "0");
                        result.SteelMark = dataTable.Rows[i][4].ToString();
                        result.IngotProfile = dataTable.Rows[i][5].ToString();
                        result.WeightOne = int.Parse(dataTable.Rows[i][6].ToString() ?? "0");
                        result.ProductCode = int.Parse(dataTable.Rows[i][7].ToString() ?? "0");
                        result.Customer = dataTable.Rows[i][8].ToString();
                        result.Standart = dataTable.Rows[i][9].ToString();

                        string diam = dataTable.Rows[i][10].ToString() ?? "0";
                        diam = diam.Replace(".", ",");
                        result.Diameter = double.Parse(diam);
                        result.Shift = dataTable.Rows[i][11].ToString();
                        result.IngotClass = dataTable.Rows[i][12].ToString();
                        result.Specification = dataTable.Rows[i][13].ToString();

                        string lot = dataTable.Rows[i][14].ToString() ?? "0";
                        if (string.IsNullOrEmpty(lot))
                            lot = "0";
                        result.Lot = int.Parse(lot);
                        result.ProductProfile = dataTable.Rows[i][15].ToString();
                        result.DiameterPrecision = dataTable.Rows[i][16].ToString();
                    }
                    catch (FormatException ex)
                    {
                        _logger.Error(
                            $"Ошибка при получении сохраненного состояния полей формы ввода [{ex.Message}]");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Открыть новый, или закрыть ранее открытый простой
        /// </summary>
        /// <param name="startTime">Время начала или окончания простоя</param>
        /// <param name="comment">Комментарий</param>
        public void SetDowntime(DateTime? startTime, [CanBeNull] string comment)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "call public.p_set_downtime(";
            
            if (startTime != null)
            {
                query += $"'{startTime:O}'";
            }
            else
            {
                DateTime start = DateTime.Now.AddMinutes(-3);
                query += $"'{start:O}'";
            }

            if (!string.IsNullOrEmpty(comment))
            {
                query += $", '{comment}'";
            }
            else
            {
                query += ", ''";
            }
            
            query += ");";

            int res = WriteData(query);
            if (res == -1)
            {
                _logger.Error($"Не удалось открыть простой с датой начала {startTime} и комментарием {comment}");
            }
        }


        /// <summary>
        /// Записать данные в таблицу БД
        /// </summary>
        /// <param name="query">SQL-запрос</param>
        /// <param name="testDb">Прзнак тестовой базы</param>
        /// <returns>Результат выполнения операции</returns>
        private int WriteData(string query, bool testDb = false)
        {
            int result = -1;

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(testDb ? _connectionStringTest : _connectionString))
                {
                    connection.Open();
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) command.ExecuteNonQuery();
                    connection.Close();
                    result = 1;
                }
            }
            catch (NpgsqlException ex)
            {
                _logger.Error($"Не удалось записать данные в базу данных: [{ex.Message}]");
            }

            return result;
        }

        /// <summary>
        /// Получить список ГОСТов
        /// </summary>
        /// <returns>Список профилей заготовок</returns>
        public List<string> GetGosts()
        {
            string query = $"set session statement_timeout to '{_timeOutP}ms'; ";
            query += "select id, gost from public.gosts order by gost;";

            List<string> result = new List<string>();
            DataTable dataTable = GetDataTable(query);


            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string value;
                    try
                    {
                        value = dataTable.Rows[i][1].ToString();
                    }
                    catch (FormatException ex)
                    {
                        value = "";
                        _logger.Error($"Ошибка при получении списка стандартов [{ex.Message}]");
                    }

                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить список профилей заготовок
        /// </summary>
        /// <returns>Список профилей заготовок</returns>
        public List<string> GetProfiles()
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "select id, profile from public.profiles order by id;";

            List<string> result = new List<string>();
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string value;
                    try
                    {
                        value = dataTable.Rows[i][1].ToString();
                    }
                    catch (FormatException ex)
                    {
                        value = "";
                        _logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                    }

                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить список заказчиков
        /// </summary>
        /// <returns>Список заказчиков</returns>
        public List<string> GetCustomers()
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "select id, customer from public.customers order by customer;";

            List<string> result = new List<string>();
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string value;
                    try
                    {
                        value = dataTable.Rows[i][1].ToString();
                    }
                    catch (FormatException ex)
                    {
                        value = "";
                        _logger.Error($"Ошибка при получении списка заказчиков [{ex.Message}]");
                    }

                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить список келассов
        /// </summary>
        /// <returns>Список заказчиков</returns>
        public List<string> GetClasses()
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "select id, class from public.classes order by class;";

            List<string> result = new List<string>();
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string value;
                    try
                    {
                        value = dataTable.Rows[i][1].ToString();
                    }
                    catch (FormatException ex)
                    {
                        value = "";
                        _logger.Error($"Ошибка при получении списка классов [{ex.Message}]");
                    }

                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить количество взвешенных заготовок по номеру плавки и диаметру
        /// </summary>
        /// <param name="meltId">Идентификатор плавки</param>
        /// <returns>Количество взвешенных заготовок</returns>
        public WeightedIngotsCount GetWeightedIngotsCount(int meltId)
        {
            WeightedIngotsCount result = new WeightedIngotsCount();
            string query = _dbQueries.GetWeightedIngotsCount(meltId, _timeOutP);
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    try
                    {
                        string val = dataTable.Rows[i][0].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        result.LandingTestId = int.Parse(val);

                        val = dataTable.Rows[i][1].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        result.Melt = val;

                        val = dataTable.Rows[i][2].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        result.LandingCount = int.Parse(val);

                        val = dataTable.Rows[i][3].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        result.WeightedCount = int.Parse(val);
                    }
                    catch (FormatException ex)
                    {
                        _logger.Error(
                            $"Не удалось прочитать количество взвешенных заготвок для плавки с идентификатором [{meltId}] - {ex.Message}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Получить список марок стали
        /// </summary>
        /// <returns>Список марок стали</returns>
        public List<string> GetSteels()
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "select id, steel from public.steels order by steel";

            DataTable dataTable = GetDataTable(query);
            List<string> result = new List<string>();

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string value;
                    try
                    {
                        value = dataTable.Rows[i][1].ToString();
                    }
                    catch (FormatException ex)
                    {
                        value = "";
                        _logger.Error($"Ошибка при получении списка марок стали [{ex.Message}]");
                    }

                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Увеличить количество заготовок в плавке по идентификатору плавки
        /// </summary>
        /// <param name="uid">Идентификатор плавки</param>
        /// <param name="testDb">Признак добавления ЕУ в тестовой БД</param>
        /// <returns>Идентификатор измененной плавки</returns>
        public int IncLanding(int uid, bool testDb=false)
        {
            int result = -1;

            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.f_add_unit({uid})";
            DataTable dataTable = GetDataTable(query, testDb);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    try
                    {
                        result = int.Parse(dataTable.Rows[i][0].ToString() ?? "-1");
                    }
                    catch (FormatException ex)
                    {
                        result = -1;
                        _logger.Error(
                            $"Ошибка при увеличении количества заготовок в плавке ({uid}) => [{ex.Message}]");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Уменьшить количество заготовок в плавке по идентификатору плавки
        /// </summary>
        /// <param name="uid">Идентификатор плавки</param>
        /// <param name="testDb">Признак удаления ЕУ на тестовой БД</param>
        /// <returns>Идентификаторр измененной плавки</returns>
        public int DecLanding(int uid, bool testDb=false)
        {
            int result = -1;

            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.f_delete_unit({uid})";
            DataTable dataTable = GetDataTable(query, testDb);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    try
                    {
                        result = int.Parse(dataTable.Rows[i][0].ToString() ?? "-1");
                    }
                    catch (FormatException ex)
                    {
                        result = -1;
                        _logger.Error(
                            $"Ошибка при уменьшении количества заготовок в плавке ({uid}) => [{ex.Message}]");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Удалить плавку из очереди
        /// </summary>
        /// <param name="uid">Идентификатор плавки</param>
        /// <param name="testDb">Признак очистки очереди на тестовой БД</param>
        /// <returns>Идентификатор удаленной плавки</returns>
        public int Remove(int uid, bool testDb=false)
        {
            int result = -1;

            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.f_delete_from_queue({uid})";
            DataTable dataTable = GetDataTable(query, testDb);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    try
                    {
                        string res = dataTable.Rows[i][0].ToString();
                        if (string.IsNullOrEmpty(res))
                        {
                            res = "1";
                        }

                        result = int.Parse(res);
                    }
                    catch (FormatException ex)
                    {
                        result = -1;
                        string dbType = testDb ? "TEST" : "PROD";
                        _logger.Error(
                            $"[{dbType}] Ошибка при получении идентификатора удаленной плавки ({uid}) => [{ex.Message}]");
                    }
                }
            }

            return result;
        }

        // /// <summary>
        // /// Получить идент плавки на тесте по иденту плавки на проде
        // /// </summary>
        // /// <param name="prodId">Идент плавки на проде</param>
        // /// <returns>Идент плавки на тесте</returns>
        // public int GetTestLandingId(int prodId)
        // {
        //     int result = -1;
        //
        //     string query = $"select unit_id from mts.passport p where p.param_id = 1 and p.value_s = {prodId}::text;";
        //     DataTable dataTable = GetDataTable(query);
        //
        //     if (dataTable.Rows.Count > 0)
        //     {
        //         for (int i = 0; i < dataTable.Rows.Count; i++)
        //         {
        //             try
        //             {
        //                 result = int.Parse(dataTable.Rows[i][0].ToString() ?? "-1");
        //             }
        //             catch (FormatException ex)
        //             {
        //                 result = -1;
        //                 _logger.Error(
        //                     $"Ошибка при получени идента плавки на тесте по идесту с прода ({prodId}) => [{ex.Message}]");
        //             }
        //         }
        //     }
        //
        //     return result;
        // }

        /// <summary>
        /// Добавить наряд в очередь посада печи 
        /// </summary>
        /// <param name="data">Данные по наряду</param>
        /// <param name="testDb">Признак добавления посада в тестовую базу</param>
        /// <returns>UID вставленной записи</returns>
        public int CreateOvenLanding(LandingData data, bool testDb = false)
        {
            string diam = data.Diameter.ToString(CultureInfo.CurrentCulture).Replace(",", ".");

            //Если добавляем арматуру, то диаметр - целое число
            if (data.ProductProfile == "№")
                diam = Math.Ceiling(data.Diameter).ToString("F0").Replace(",", ".");
            else
            {
                if (data.DiameterPrecision == "1")
                    diam = data.Diameter.ToString("F1").Replace(",", ".");
                else if (data.DiameterPrecision == "2")
                    diam = data.Diameter.ToString("F2").Replace(",", ".");
            }

            string query = "set session statement_timeout to ";
            query += testDb ? $"'{_timeOutT}ms';" : $"'{_timeOutP}ms';";
            query +=
                $"SELECT public.f_create_queue ('{data.MeltNumber}', '{data.IngotProfile}', '{data.SteelMark}', " +
                $"{data.IngotsCount}, {data.WeightAll}, {data.WeightOne}, {data.IngotLength}, '{data.Standart}', " +
                $"{diam}, '{data.Customer}', '{data.Shift}', '{data.IngotClass}', {data.ProductCode}, '{data.ProductProfile}');";

            DataTable dataTable = GetDataTable(query, testDb);
            int result = -1;

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    try
                    {
                        result = int.Parse(dataTable.Rows[i][0].ToString() ?? "-1");
                    }
                    catch (FormatException ex)
                    {
                        result = -1;
                        _logger.Error(
                            $"Ошибка при добавлении плавки №({data.MeltNumber}) в очередь [{ex.Message}]");
                    }
                }
            }

            return result;
        }

                /// <summary>
        /// Получить идент плавки на тесте по иденту плавки на проде
        /// </summary>
        /// <param name="prodId">Идент плавки на проде</param>
        /// <returns>Идент плавки на тесте</returns>
        public int GetTestLandingId(int prodId)
        {
            int result = -1;

            string query = $"select unit_id from mts.passport p where p.param_id = 1 and p.value_s = {prodId}::text;";
            DataTable dataTable = GetDataTable(query, true);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    try
                    {
                        result = int.Parse(dataTable.Rows[i][0].ToString() ?? "-1");
                    }
                    catch (FormatException ex)
                    {
                        result = -1;
                        _logger.Error(
                            $"Ошибка при получени идента плавки на тесте по идесту с прода ({prodId}) => [{ex.Message}]");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Установить соответствие плавки на проде и на тесте
        /// </summary>
        /// <param name="prodId">Идент плавки на проде</param>
        /// <param name="testId">Идент плавки на тесте</param>
        public bool SetRelation(int prodId, string testId)
        {
            // При вставке посада на тесте - добавить параметр, связывающий ид-р посада на тесте и на проде
            // call public.p_set_param(p_unit_id, p_param_id, p_value);
            // p_unit_id - ид-р посада на тесте (число);
            // p_param_id - =1;
            // p_value - ид-р посада на проде (строка);
            
            string query = $"call public.p_set_param({testId}, 1, '{prodId}');";
            bool result = false;

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionStringTest))
                {
                    connection.Open();
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                        command.ExecuteNonQuery();
                    connection.Close();
                    
                    _logger.Info($"Установлено соответствие плавки между тестом [{testId}] и продом [{prodId}]");
                    result = true;
                }
            }
            catch (NpgsqlException ex)
            {
                _logger.Error($"Не удалось установить соответствие плавки между тестом [{testId}] и продом [{prodId}]: [{ex.Message}]");
            }

            return result;
        }

        /// <summary>
        /// Получить список возвратов по номеру плавки
        /// </summary>
        /// <param name="melt">Номер плавки</param>
        /// <returns>Список возвратов</returns>
        public List<ReturningData> GetReturns(string melt)
        {
            string query = _dbQueries.GetReturnsByMelt(melt, _timeOutP);
            List<ReturningData> result = _getReturns(query);

            return result;
        }

        /// <summary>
        /// Получить список возвратов по готовому запросу
        /// </summary>
        /// <param name="query">Запрос</param>
        /// <returns>Список возвратов</returns>
        private List<ReturningData> _getReturns(string query)
        {
            List<ReturningData> result = new List<ReturningData>();
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ReturningData item = new ReturningData();
                    try
                    {
                        string val = dataTable.Rows[i][0].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.Melt = val;

                        val = dataTable.Rows[i][1].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = DateTime.MinValue.ToString("G");
                        item.TimeBegin = DateTime.Parse(val);

                        val = dataTable.Rows[i][2].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = DateTime.MinValue.ToString("G");
                        item.TimeEnd = DateTime.Parse(val);

                        val = dataTable.Rows[i][3].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.IngotNumber = int.Parse(val);

                        val = dataTable.Rows[i][4].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.IngotsCount = int.Parse(val);

                        val = dataTable.Rows[i][5].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = DateTime.MinValue.ToString("G");
                        item.TimeCreateLanding = DateTime.Parse(val);

                        val = dataTable.Rows[i][6].ToString()?.Trim();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.IngotWeight = int.Parse(val);
                    }
                    catch (FormatException ex)
                    {
                        _logger.Error(
                            $"Не удалось прочитать список возвратов по готовому запросу [{ex.Message}]");
                    }

                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить список ЕУ на весах перед печью
        /// </summary>
        /// <returns>Список ЕУ на весах перед печью</returns>
        public List<CoilData> GetIngotOnScales()
        {
            List<CoilData> result = new List<CoilData>();
            string query = _dbQueries.GetIngotOnScales;
            DataTable dataTable = GetDataTable(query);

            foreach (DataRow row in dataTable.Rows)
            {
                CoilData item = new CoilData();

                try
                {
                    string val = row[0].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.CoilUid = int.Parse(val);
                    
                    val = row[1].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.CoilPos = int.Parse(val);
                    
                    val = row[2].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = DateTime.Now.ToString("G");
                    item.DateWeight = DateTime.Parse(val);
                    
                    val = row[3].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.WeightFact = int.Parse(val);
                    
                    val = row[4].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.IngotPos = int.Parse(val);
                    
                    val = row[5].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.PosadUid = int.Parse(val);
                    
                    val = row[6].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.MeltNumber = val;
                    
                    val = row[7].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.SteelMark = val;
                    
                    val = row[8].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.IngotProfile = val;
                    
                    val = row[9].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.IngotsCount = int.Parse(val);
                    
                    val = row[10].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.WeightAll = int.Parse(val);
                    
                    val = row[11].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.WeightOne = int.Parse(val);
                    
                    val = row[12].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.IngotLength = int.Parse(val);
                    
                    val = row[13].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.Standart = val;
                    
                    val = row[14].ToString()?.Trim().Replace(".", ",");
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.Diameter = double.Parse(val);
                    
                    val = row[15].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.Customer = val;
                    
                    val = row[16].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.Shift = val;
                    
                    val = row[17].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.Class = val;
                    
                    val = row[18].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.ProductionCode = int.Parse(val);
                    
                    val = row[19].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.ProductionProfile = val;
                    
                    val = row[20].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.IngotsWeighted = int.Parse(val);
                }
                catch (FormatException ex)
                {
                    _logger.Error(
                        $"Не удалось прочитать список ЕУ на весах перед печью [{ex.Message}]");
                }
                
                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Получить список посадов в печи
        /// </summary>
        /// <returns>Список посадов в печи</returns>
        public List<IngotsInOven> GetMeltsInOven()
        {
            List<IngotsInOven> result = new List<IngotsInOven>();
            string query = _dbQueries.GetIngotsInOven;

            DataTable dataTable = GetDataTable(query);

            foreach (DataRow row in dataTable.Rows)
            {
                IngotsInOven item = new IngotsInOven();

                try
                {
                    string val = row[0].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = DateTime.MinValue.ToString("G");
                    item.TimeEnter = DateTime.Parse(val);
                    
                    val = row[1].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.LandingId = int.Parse(val);
                    
                    val = row[2].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.MeltNumber = val;
                    
                    val = row[3].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.IngotsCount = int.Parse(val);
                    
                    val = row[4].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.SteelMark = val;
                    
                    val = row[5].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "150x150";
                    item.IngotProfile = val;
                    
                    val = row[6].ToString()?.Trim().Replace(".", ",");
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.Diameter = double.Parse(val);
                    
                    val = row[7].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.Customer = val;
                    
                    val = row[8].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.ProductProfile = val;
                    
                    val = row[9].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.IngotsCountInOven = int.Parse(val);
                    
                    val = row[10].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.IngotsCountOutOven = int.Parse(val);
                    
                    val = row[11].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.IngotsWeighted = int.Parse(val);
                    
                    val = row[12].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.ProductCode = int.Parse(val);
                    
                    val = row[13].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.StartPos = int.Parse(val);
                    
                    val = row[14].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.EndPos = int.Parse(val);
                }
                catch (FormatException ex)
                {
                    _logger.Error(
                        $"Не удалось прочитать список ЕУ на весах перед печью [{ex.Message}]");
                }

                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Получить список нарядов заготовок на посаде печи
        /// </summary>
        /// <returns>Список нарядов на посад в печь</returns>
        public List<LandingData> GetLandingOrder(string melt = "", double diameter = 0.0)
        {
            List<LandingData> result = new List<LandingData>();

            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += string.IsNullOrEmpty(melt)
                ? "select * from public.f_get_queue();"
                : $"select * from public.f_get_queue() where c_melt='{melt}' and c_diameter={diameter.ToString("F1").Replace(",", ".")};";

            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    LandingData item = new LandingData();
                    try
                    {
                        item.LandingId = int.Parse(dataTable.Rows[i][0].ToString() ?? "0");
                        item.MeltNumber = dataTable.Rows[i][1].ToString();
                        item.SteelMark = dataTable.Rows[i][2].ToString();
                        item.IngotProfile = dataTable.Rows[i][3].ToString();
                        item.IngotsCount = int.Parse(dataTable.Rows[i][4].ToString() ?? "0");
                        item.WeightAll = int.Parse(dataTable.Rows[i][5].ToString() ?? "0");
                        item.WeightOne = int.Parse(dataTable.Rows[i][6].ToString() ?? "0");
                        item.IngotLength = int.Parse(dataTable.Rows[i][7].ToString() ?? "0");
                        item.Standart = dataTable.Rows[i][8].ToString();

                        string diam = dataTable.Rows[i][9].ToString() ?? "0";
                        diam = diam.Replace(".", ",");
                        item.Diameter = double.Parse(diam);

                        item.Customer = dataTable.Rows[i][10].ToString();
                        item.Shift = dataTable.Rows[i][11].ToString();
                        item.IngotClass = dataTable.Rows[i][12].ToString();
                        item.ProductCode = int.Parse(dataTable.Rows[i][13].ToString() ?? "0");
                        item.ProductProfile = dataTable.Rows[i][14].ToString();
                        item.WeightedIngots = int.Parse(dataTable.Rows[i][15].ToString() ?? "0");
                    }
                    catch (FormatException ex)
                    {
                        _logger.Error(
                            $"Ошибка при получении списка очереди заготовок на посаде печи [{ex.Message}]");
                    }

                    result.Add(item);
                }
            }

            return result;
        }

        public List<CoilData> GetCoilData(bool current=true, bool last=true)
        {
            List<CoilData> result = new List<CoilData>();
            
            if (current)
            {
                // Получить список бунтов для текущей плавки
                List<LandingData> landed = GetLandingOrder();
                foreach (LandingData item in landed)
                {
                    if (item.Weighted > 0)
                    {
                        result = GetCoilsByMelt(item.MeltNumber, item.Diameter, last);
                    }
                }
            }
            else
            {
                // Получить список бунтов для предыдущей плавки
                Dictionary<string, double> previous = GetPreviousMeltNumber();
                foreach (KeyValuePair<string, double> melt in previous)
                {
                    if (!string.IsNullOrEmpty(melt.Key) && melt.Value > 0)
                    {
                        result = GetCoilsByMelt(melt.Key, melt.Value, last);
                    }
                }
            }

            return result;
        }

        private Dictionary<string, double> GetPreviousMeltNumber()
        {
            string query = $"set session statement_timeout to '{_timeOutP}ms'; ";
            query += "call public.p_get_previos_melt(null, null);";

            Dictionary<string, double> result = new Dictionary<string, double>();
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string key = "";
                    double value = 0;

                    try
                    {
                        string val = dataTable.Rows[i][0].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        key = val;

                        val = dataTable.Rows[i][1].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        val = val.Replace(".", ",");
                        value = double.Parse(val);
                    }
                    catch (FormatException ex)
                    {
                        _logger.Error(
                            $"Не удалось получить номер и диаметр предыдущей провешеной плавки [{ex.Message}]");
                    }

                    result.Add(key, value);
                }
            }

            return result;
        }

        public List<CoilData> GetCoilsByMelt(string melt, double diameter, bool last = true)
        {
            List<CoilData> result = new List<CoilData>();
            string diam = diameter.ToString("F1").Replace(",", ".");
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";

            if (!last)
            {
                query += $"select * from public.f_get_queue_coils('{melt}', {diam});";
            }
            else
            {
                query +=
                    $"select * from public.f_get_queue_coils('{melt}', {diam}) order by c_date_weight desc limit 1;";
            }

            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    CoilData item = new CoilData();
                    try
                    {
                        string val = dataTable.Rows[i][1].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.MeltNumber = val;

                        val = dataTable.Rows[i][9].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = " ";
                        item.ProductionProfile = val;

                        val = dataTable.Rows[i][10].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        val = val.Replace(".", ",");
                        item.Diameter = double.Parse(val);

                        val = dataTable.Rows[i][15].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.CoilUid = int.Parse(val);

                        val = dataTable.Rows[i][16].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.CoilPos = int.Parse(val);

                        val = dataTable.Rows[i][17].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.CoilNumber = int.Parse(val);

                        val = dataTable.Rows[i][18].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.WeightFact = int.Parse(val);

                        val = dataTable.Rows[i][19].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.ShiftNumber = val;

                        val = dataTable.Rows[i][22].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = DateTime.MinValue.ToString("G");
                        item.DateReg = DateTime.Parse(val);

                        val = dataTable.Rows[i][23].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = DateTime.MinValue.ToString("G");
                        item.DateWeight = DateTime.Parse(val);

                    }
                    catch (FormatException ex)
                    {
                        _logger.Error(
                            $"Не удалось получить список бунтов для плавки №{melt} с диаметром {diam} [{ex.Message}]");
                    }

                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Вернуть бунт в очередь на взвешивание
        /// </summary>
        /// <param name="coilUid">Идентификатор бунта</param>
        public void ResetCoil(int coilUid)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.f_return_to_queue({coilUid});";
            DataTable dataTable = GetDataTable(query);

            if (dataTable.Rows.Count > 0)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    CoilData item = new CoilData();
                    try
                    {
                        string val = dataTable.Rows[i][0].ToString();
                        if (string.IsNullOrEmpty(val))
                            val = "0";
                        item.CoilUid = int.Parse(val);
                        _logger.Info($"Произведен сброс веса бунта [{val}]");
                    }
                    catch (FormatException ex)
                    {
                        _logger.Error(
                            $"Не удалось сбросить вес бунта {coilUid} [{ex.Message}]");
                    }
                }
            }
        }

        /// <summary>
        /// Добавить профиль заготовки
        /// </summary>
        /// <param name="profileName">Профиль заготовки</param>
        /// <returns>Результат выполнения операции</returns>
        public int AddProfile(string profileName)
        {
            int res = 0;
            if (!string.IsNullOrEmpty(profileName))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.profiles (profile) values ('{profileName}'); ";
                
                int resP = WriteData(query);
                int resT = WriteData(query, true) * 10;

                res += resP;
                res += resT;
            }

            return res;
        }
        
        /// <summary>
        /// Добавить марку стали
        /// </summary>
        /// <param name="steelName">Марка стали</param>
        /// <returns>Результат выполнения операции</returns>
        public int AddSteel(string steelName)
        {
            int res = 0;
            if (!string.IsNullOrEmpty(steelName))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.steels (steel) values ('{steelName}'); ";
                
                int resP = WriteData(query);
                int resT = WriteData(query, true) * 10;

                res += resP;
                res += resT;
            }

            return res;
        }
        
        /// <summary>
        /// Добавить ГОСТ
        /// </summary>
        /// <param name="gostName">ГОСТ</param>
        /// <returns>Результат выполнения операции</returns>
        public int AddGost(string gostName)
        {
            int res = 0;
            if (!string.IsNullOrEmpty(gostName))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.gosts (gost) values ('{gostName}'); ";
                
                int resP = WriteData(query);
                int resT = WriteData(query, true) * 10;

                res += resP;
                res += resT;
            }

            return res;
        }
        
        /// <summary>
        /// Добавить заказчика
        /// </summary>
        /// <param name="customerName">Заказчик</param>
        /// <returns>Результат выполнения операции</returns>
        public int AddCustomer(string customerName)
        {
            int res = 0;
            if (!string.IsNullOrEmpty(customerName))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms';";
                query += $"insert into public.customers (customer) values ('{customerName}');";
                
                int resP = WriteData(query);
                int resT = WriteData(query, true) * 10;

                res += resP;
                res += resT;
            }

            return res;
        }
        
        /// <summary>
        /// Добавить класс
        /// </summary>
        /// <param name="className">Класс</param>
        /// <returns>Результат выполнения операции</returns>
        public int AddClass(string className)
        {
            int res = 0;
            if (!string.IsNullOrEmpty(className))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.classes (class) values ('{className}'); ";
                
                int resP = WriteData(query);
                int resT = WriteData(query, true) * 10;

                res += resP;
                res += resT;
            }

            return res;
        }

        /// <summary>
        /// Получить таблицу данных из БД по запросу
        /// </summary>
        /// <param name="query">Текст запроса</param>
        /// <param name="testDb">Признак выполнения запроса к тестовой БД</param>
        /// <returns>Таблица данных</returns>
        private DataTable GetDataTable(string query, bool testDb=false)
        {
            DataTable result = new DataTable();

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(testDb ? _connectionStringTest : _connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(result);
                    connection.Close();
                }
            }
            catch (NpgsqlException ex)
            {
                string dbType = testDb ? "TEST" : "PROD";
                _logger.Error($"[{dbType}] Не удалось подключиться к базе данных: [{ex.Message}]");
            }

            return result;
        }

        /// <summary>
        ///  Получить список парметров посада
        /// </summary>
        /// <param name="landingId">Идентификатор посада</param>
        /// <returns>Список параметров посада</returns>
        public List<PassportParam> GetPassportParams(int landingId)
        {
            List<PassportParam> result = new List<PassportParam>();
            string query = _dbQueries.GetMeltParams(landingId);
            DataTable dataTable = GetDataTable(query);

            foreach (DataRow row in dataTable.Rows)
            {
                PassportParam item = new PassportParam();

                try
                {
                    string val = row[0].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "0";
                    item.ParamId = int.Parse(val);

                    val = row[1].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.ParamName = val;

                    // val = row[2].ToString()?.Trim().Replace(".", ",");
                    // if (string.IsNullOrEmpty(val))
                    //     val = "0";
                    // item.ValueNumeric = int.Parse(val);

                    val = row[3].ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                        val = "";
                    item.ValueString = val;
                }
                catch (FormatException ex)
                {
                    _logger.Error($"Не удалось прочитать параметры плавки [{landingId}] => {ex.Message}");
                }
                
                result.Add(item);
            }

            return result;
        }
    }
}
