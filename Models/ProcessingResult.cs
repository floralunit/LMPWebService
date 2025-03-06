namespace LMPWebService.Models
{
    public class ProcessingResult
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }

        private ProcessingResult(bool success, string? errorMessage = null)
        {
            IsSuccess = success;
            ErrorMessage = errorMessage;
        }

        public static ProcessingResult Success() => new ProcessingResult(true);
        public static ProcessingResult Failure(string errorMessage) => new ProcessingResult(false, errorMessage);
    }

}
