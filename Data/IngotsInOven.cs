using System;

namespace OvenLanding.Data
{
    public class IngotsInOven
    {
        public DateTime TimeEnter { get; set; }
        public int LandingId { get; set; }
        public string MeltNumber { get; set; }
        public int IngotsCount { get; set; }
        public string SteelMark { get; set; }
        public string IngotProfile { get; set; }
        public double Diameter { get; set; }
        public string DisplayDiameter { get; set; }
        public string Customer { get; set; }
        public string ProductProfile { get; set; }
        public int IngotsCountInOven { get; set; }
        public int IngotsCountOutOven { get; set; }
        public int ProductCode { get; set; }
        public int IngotsWeighted { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }

        public IngotsInOven()
        {
            TimeEnter = DateTime.MinValue;
            LandingId = 0;
            MeltNumber = "";
            IngotsCount = 0;
            SteelMark = "";
            IngotProfile = "";
            Diameter = 0;
            DisplayDiameter = "";
            Customer = "";
            ProductProfile = "";
            IngotsCountInOven = 0;
            IngotsCountOutOven = 0;
            ProductCode = 0;
            IngotsWeighted = 0;
            StartPos = 0;
            EndPos = 0;
        }
    }
}