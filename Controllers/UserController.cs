using Microsoft.AspNetCore.Mvc;
using ZoraVault.Models.Internal;
using ZoraVault.Services;
using ZoraVault.Models.DTOs.Users;
using ZoraVault.Models.Internal.Common;

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
        private readonly AuthService _authService;  // Service responsible for user registration and authentication

        /// <summary>
        /// Constructor injects AuthService dependency.
        /// </summary>
        /// <param name="authService">The authentication service handling user-related business logic.</param>
        public UserController(AuthService authService)
        {
            _authService = authService;
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
    }
}
