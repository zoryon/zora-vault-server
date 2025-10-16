using System.Text.Json;

namespace ZoraVault.Models.Common
{
    public class ApiResponse<T>
    {
        // Properties
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        // Constructors
        public ApiResponse() { }

        public ApiResponse(bool success, int statusCode, string message, T? data = default, IEnumerable<string>? errors = null)
        {
            Success = success;
            StatusCode = statusCode;
            Message = message;
            Data = data;
            Errors = errors;
        }

        // Generic factory methods for common responses
        public static ApiResponse<T> SuccessResponse(T data, int statusCode = 200, string message = "Success")
        {
            return new ApiResponse<T>(true, statusCode, message, data);
        }

        public static ApiResponse<T> ErrorResponse(int statusCode = 500, string message = "Server error", List<string>? errors = null)
        {
            return new ApiResponse<T>(false, statusCode, message, default, errors);
        }

        // Common HTTP Responses
        public static ApiResponse<T> Created(T data, string message = "Successfully created")
        {
            return new ApiResponse<T>(true, 201, message, data);
        }

        public static ApiResponse<T> BadRequest(IEnumerable<string>? errors = null, string message = "Bad Request")
        {
            return new ApiResponse<T>(false, 400, message, default, errors);
        }

        public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
        {
            return new ApiResponse<T>(false, 401, message);
        }

        public static ApiResponse<T> Forbidden(string message = "Forbidden")
        {
            return new ApiResponse<T>(false, 403, message);
        }

        public static ApiResponse<T> NotFound(string message = "Not Found")
        {
            return new ApiResponse<T>(false, 404, message);
        }
    }
}
