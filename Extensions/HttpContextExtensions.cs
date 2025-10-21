using ZoraVault.Models.Internal.Common;

namespace ZoraVault.Extensions
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Retrieves the authenticated user ID and device ID stored in HttpContext.Items by the AuthMiddleware.
        /// Throws UnauthorizedAccessException if not present or invalid.
        /// </summary>
        public static AuthContext GetAuthContext(this HttpContext context)
        {
            if (context.Items.TryGetValue("ReqUserId", out var userIdObj) &&
                context.Items.TryGetValue("ReqDeviceId", out var deviceIdObj) &&
                userIdObj is Guid userId &&
                deviceIdObj is Guid deviceId)
            {
                return new AuthContext(userId, deviceId);
            }

            throw new UnauthorizedAccessException("Missing or invalid user/device information in HttpContext.");
        }
    }
}
