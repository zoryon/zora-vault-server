using Microsoft.AspNetCore.Mvc;
using ZoraVault.Models.Internal;
using ZoraVault.Services;
using ZoraVault.Models.DTOs.Users;
using ZoraVault.Models.Internal.Common;
using ZoraVault.Extensions;

namespace ZoraVault.Controllers
{
    /// <summary>
    /// Handles all API endpoints related to user accounts.
    /// This includes:
    /// - User registration
    /// </summary>
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AuthService _authService;  // Service responsible for auth operations
        private readonly UserService _userService;  // Service responsible for user operations

        /// <summary>
        /// Constructor injects AuthService dependency.
        /// </summary>
        /// <param name="authService">The authentication service handling user-related business logic.</param>
        public UserController(AuthService authService, UserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        // ---------------------------------------------------------------------------
        // POST /api/users/me
        // ---------------------------------------------------------------------------
        [HttpGet("me")]
        public async Task<ApiResponse<PublicUser>> GetCurrentUserAsync()
        {
            AuthContext ctx = HttpContext.GetAuthContext();
            PublicUser user = await _userService.FetchCurrentUserAsync(ctx.UserId);

            return ApiResponse<PublicUser>.SuccessResponse(user);
        }

        // ---------------------------------------------------------------------------
        // POST /api/users
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Registers a new user with the provided registration details.
        /// </summary>
        [HttpPost]
        public async Task<ApiResponse<PublicUser>> RegisterUser([FromBody] UserRegistrationRequest req)
        {
            // Delegate user registration to the AuthService (handles all validations and hashing)
            PublicUser user = await _authService.RegisterUserAsync(req);

            return ApiResponse<PublicUser>.Created(user);
        }

        // ---------------------------------------------------------------------------
        // PATCH /api/users/me
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Update some fields of a user with the provided details.
        /// </summary>
        [HttpPatch("me")]
        public async Task<ApiResponse<PublicUser>> UpdateCurrentUserAsync([FromBody] PatchUserRequest req)
        {
            AuthContext ctx = HttpContext.GetAuthContext();
            PublicUser user = await _userService.ReplaceFieldsCurrentUserAsync(ctx.UserId, req);

            return ApiResponse<PublicUser>.SuccessResponse(user, 200, "Fields were updated successfully");
        }

        // ---------------------------------------------------------------------------
        // PUT /api/users/me/settings
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Update all fields of a user settings with the provided details.
        /// </summary>
        [HttpPut("me/settings")]
        public async Task<ApiResponse<UpdateUserSettingsResponse>> UpdateCurrentUserAsync([FromBody] UpdateUserSettingsRequest req)
        {
            AuthContext ctx = HttpContext.GetAuthContext();
            UpdateUserSettingsResponse usr = await _userService.ReplaceCurrentUserSettingsAsync(ctx.UserId, ctx.DeviceId, req);

            return ApiResponse<UpdateUserSettingsResponse>.SuccessResponse(usr, 200, "Fields were updated successfully");
        }
    }
}
