namespace ZoraVault.Models.DTOs
{
    public class Response(string message = "Success", int statusCode = 200, dynamic data = null!)
    {
        public string Message { get; set; } = message;
        public int StatusCode { get; set; } = statusCode;
        public dynamic Data { get; set; } = data;
    }
}
