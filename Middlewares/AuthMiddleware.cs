using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ZoraVault.Configuration;

namespace ZoraVault.Middlewares
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Secrets _secrets;

        public AuthMiddleware(RequestDelegate next, Secrets secrets)
        {
            _next = next;
            _secrets = secrets;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.Value ?? "";
            string method = context.Request.Method;

            // Skip excluded endpoints (check path + method)
            if (Endpoints.PublicEndpoints.Any(ep =>
                path.StartsWith(ep.Path, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ep.Method, method, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Get the Authorization header
            string? authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing or invalid Authorization header.");
                return;
            }

            string token = authHeader["Bearer ".Length..].Trim();

            try
            {
               var claims = Helpers.SecurityHelpers.ValidateJWT(token, _secrets.AccessTokenSecret)
                    ?? throw new SecurityTokenException("Invalid token");

                // Extract userId and deviceId from claims
                string? userIdStr = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                string? deviceIdStr = claims.FindFirst("deviceId")?.Value;

                if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(deviceIdStr))
                    throw new SecurityTokenException("Invalid token: missing claims");

                // Convert IDs from string to Guid
                if (!Guid.TryParse(userIdStr, out Guid userId) || !Guid.TryParse(deviceIdStr, out Guid deviceId))
                    throw new SecurityTokenException("Invalid token");

                // Store userId and deviceId in HttpContext for downstream use
                context.Items["ReqUserId"] = userId;
                context.Items["ReqDeviceId"] = deviceId;

                // Proceed to the next middleware
                await _next(context);
            }
            catch
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid or expired token.");
                return;
            }

            // Continue if authorized
            await _next(context);
        }
    }
}
