namespace Senstay.Dojo.Models.HelperClass
{
    public class ProcessResult
    {
        public int Count { get; set; } = 0;
        public int Good { get; set; } = 0;
        public int Bad { get; set; } = 0;
        public int Skip { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
    }
}