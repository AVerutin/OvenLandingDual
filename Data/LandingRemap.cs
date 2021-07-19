namespace OvenLanding.Data
{
    public class LandingRemap
    {
        public LandingData Landing { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }

        public LandingRemap()
        {
            Landing = new LandingData();
            StartPos = 0;
            EndPos = 0;
        }
    }
}