namespace OvenLanding.Data
{
    public class PassportParam
    {
        public int ParamId { get; set; }
        public string ParamName { get; set; }
        public int ValueNumeric { get; set; }
        public string ValueString { get; set; }

        public PassportParam()
        {
            ParamId = 0;
            ParamName = "";
            ValueNumeric = 0;
            ValueString = "";
        }

        public override string ToString()
        {
            return $"[{ParamId}] => {ParamName} = {ValueString}";
        }
    }
}