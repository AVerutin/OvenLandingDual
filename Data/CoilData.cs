using System;

namespace OvenLanding.Data
{
    public class CoilData
    {
        public int PosadUid { get; set; }            // c_id_posad [numeric]
        public string MeltNumber { get; set; }       // c_melt [text]
        public string SteelMark { get; set; }        // c_steel_grade [text]
        public string IngotProfile { get; set; }     // c_section [text]
        public int IngotsCount { get; set; }         // c_count [numeric]
        public int WeightAll { get; set; }           // c_weight_all [numeric]
        public int WeightOne { get; set; }           // c_weight_one [numeric]
        public int IngotLength { get; set; }         // c_length [numeric]
        public string Standart { get; set; }         // c_gost [text]
        public double Diameter { get; set; }         // c_diameter [numeric]
        public string DisplayDiameter { get; set; }  // Отображаемое представление диаметра 
        public string Customer { get; set; }         // c_customer [text]
        public string Shift { get; set; }            // c_shift [text]
        public string Class { get; set; }            // c_class [text]
        public int ProductionCode { get; set; }      // c_prod_code [numeric]

        public int CoilUid { get; set; }             // c_id_coil [numeric], -- идентификатор бунта
        public int CoilPos { get; set; }             // c_pos [numeric], -- номер пп внутри посада
        public int IngotPos { get; set; }            // Позиция ЕУ на взвешивании перед печами
        public int CoilNumber { get; set; }          // c_num_coil [numeric],-- номер бунта, присвоенный при взвешивании (начинается со 101)
        public int WeightFact { get; set; }          // c_weight_fact [numeric], -- вес фактический
        public string ShiftNumber { get; set; }      // c_shift_number [text], -- номер бригады
        public string ProductionProfile { get; set; }// c_profile [text], -- профиль готовой продукции
        public DateTime DateReg { get; set; }        // c_date_reg [timestamp], -- дата регистрации посада
        public DateTime DateWeight { get; set; }     // c_date_weight [timestamp] -- время взвешивания
        public int IngotsInKiln { get; set; }        // Заготовок в печи
        public int IngotsOutKiln { get; set; }       // Заготовок выдано из печи
        public int IngotsWeighted { get; set; }      // Количество взвешенных заготовок 

        public CoilData()
        {
            PosadUid = 0;
            MeltNumber = "";
            SteelMark = "";
            IngotProfile = "";
            IngotsCount = 0;
            WeightAll = 0;
            WeightOne = 0;
            IngotLength = 0;
            Standart = "";
            Diameter = 0;
            DisplayDiameter = "";
            Customer = "";
            Shift = "";
            Class = "";
            ProductionCode = 0;
            CoilUid = 0;
            CoilPos = 0;
            IngotPos = 0;
            CoilNumber = 0;
            WeightFact = 0;
            ShiftNumber = "";
            DateReg = DateTime.MinValue;
            DateWeight = DateTime.MinValue;
            ProductionProfile = "";
            IngotsInKiln = 0;
            IngotsOutKiln = 0;
        }
    }
}
