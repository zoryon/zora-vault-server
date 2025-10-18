using Microsoft.AspNetCore.Mvc;
using ZoraVault.Models.Common;
using ZoraVault.Models.DTOs;
using ZoraVault.Services;

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
        public async Task<ApiResponse<PublicUserDTO>> RegisterUser([FromBody] UserRegistrationReqDTO req)
        {
            // Delegate user registration to the AuthService (handles all validations and hashing)
            PublicUserDTO user = await _authService.RegisterUserAsync(req);

            return ApiResponse<PublicUserDTO>.Created(user);
        }
    }
}
