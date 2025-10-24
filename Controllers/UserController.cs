using Microsoft.AspNetCore.Mvc;
using ZoraVault.Models.Internal;
using ZoraVault.Services;
using ZoraVault.Models.DTOs.Users;
using ZoraVault.Models.Internal.Common;
using ZoraVault.Extensions;

namespace ZoraVault.Controllers
{
    /// <summary>
    /// The UserController handles all API endpoints related to user accounts.
    /// Responsibilities include:
    /// - Retrieving the current user
    /// - User registration
    /// - Updating user fields
    /// - Updating device-specific user settings
    /// </summary>
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AuthService _authService;  // Service responsible for auth operations
        private readonly UserService _userService;  // Handles user data fetching and settings updates

        /// <summary>
        /// Constructor injects required services.
        /// </summary>
        /// <param name="authService">The authentication service for registration and security operations.</param>
        /// <param name="userService">The user service for fetching and updating user data.</param>
        public UserController(AuthService authService, UserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        // ---------------------------------------------------------------------------
        // GET /api/users/me
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Retrieves information about the currently authenticated user.
        /// </summary>
        /// <returns>A <see cref="PublicUser"/> DTO containing public user data.</returns>
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
        /// Registers a new user using the provided registration request.
        /// Performs validations, password hashing, and stores the new user in the database.
        /// </summary>
        /// <param name="req">User registration details.</param>
        /// <returns>A <see cref="PublicUser"/> DTO containing the newly created user's public info.</returns>
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
        /// Updates selective fields of the current user, such as username or encrypted vault blob.
        /// </summary>
        /// <param name="req">DTO containing fields to update.</param>
        /// <returns>The updated <see cref="PublicUser"/> object.</returns>
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
        /// Fully replaces the current user's device-specific settings.
        /// Updates all fields of UserSettings for the authenticated device.
        /// </summary>
        /// <param name="req">DTO containing all new user settings.</param>
        /// <returns>A <see cref="UpdateUserSettingsResponse"/> representing updated settings.</returns>
        [HttpPut("me/settings")]
        public async Task<ApiResponse<UpdateUserSettingsResponse>> UpdateCurrentUserAsync([FromBody] UpdateUserSettingsRequest req)
        {
            AuthContext ctx = HttpContext.GetAuthContext();
            UpdateUserSettingsResponse usr = await _userService.ReplaceCurrentUserSettingsAsync(ctx.UserId, ctx.DeviceId, req);

            return ApiResponse<UpdateUserSettingsResponse>.SuccessResponse(usr, 200, "Fields were updated successfully");
        }


        // ---------------------------------------------------------------------------
        // DELETE /api/users/me
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Deletes the currently authenticated user's account.
        /// Requires re-authentication for security.
        /// </summary>
        [HttpPost("me")]
        public async Task<ApiResponse<Guid>> DeleteCurrentUserAccountAsync([FromBody] UserAuthRequest req)
        {
            // Authenticate the user
            PublicUser user = await _authService.AuthenticateUserAsync(req)
                ?? throw new UnauthorizedAccessException("Invalid credentials");

            Guid removedId = await _userService.RemoveCurrentUserAccountAsync(user.Id);

            return ApiResponse<Guid>.SuccessResponse(removedId, 200, "User account and all related data were deleted successfully");
        }
    }
}
