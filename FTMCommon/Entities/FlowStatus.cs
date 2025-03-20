namespace FTMCommon.Entities
{
    public class FlowStatus
    {
        public bool IsError { get; set; }
        public List<FlowError> ErrorMessages { get; set; } = new List<FlowError>();
    }
    public class FlowError
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
