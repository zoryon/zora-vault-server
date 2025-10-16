using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security;

namespace ZoraVault.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            int statusCode = ex switch
            {
                // Resource not found
                KeyNotFoundException => StatusCodes.Status404NotFound,

                // Authentication/authorization
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                SecurityException => StatusCodes.Status403Forbidden,   // forbidden action

                // Conflict or duplicate resources
                DuplicateNameException => StatusCodes.Status409Conflict,
                DbUpdateConcurrencyException => StatusCodes.Status409Conflict, // EF Core concurrency issues

                // Bad request / invalid input
                ArgumentException => StatusCodes.Status400BadRequest,
                FormatException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                ValidationException => StatusCodes.Status400BadRequest, // Data annotations validation

                // External service / network errors
                TimeoutException => StatusCodes.Status504GatewayTimeout,
                HttpRequestException => StatusCodes.Status502BadGateway,

                // Not implemented / unsupported
                NotImplementedException => StatusCodes.Status501NotImplemented,
                NotSupportedException => StatusCodes.Status405MethodNotAllowed, // Method not allowed

                // Default: unexpected error
                _ => StatusCodes.Status500InternalServerError
            };


            var response = new
            {
                message = ex.Message,
                errorType = ex.GetType().Name
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
