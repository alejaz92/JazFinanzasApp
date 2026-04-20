namespace JazFinanzasApp.API.Business.DTOs.Error
{
    public class ErrorResponseDTO
    {
        public int StatusCode { get; set; }
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
