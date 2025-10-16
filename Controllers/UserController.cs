using Microsoft.AspNetCore.Mvc;
using System.Data;
using ZoraVault.Models.Common;
using ZoraVault.Models.DTOs;
using ZoraVault.Services;

namespace ZoraVault.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AuthService _authService;

        public UserController(AuthService authService)
        {
            _authService = authService;
        }

        /* 
         * POST /api/users HTTP/1.1
         * Content-Type: application/json
         * 
         * Request body (application/json):
         * {
         *   "username": "zoryon",
         *   "email": "zoryon@example.com",
         *   "passwordHash": "<client-derived value>",
         *   "kdfParams": {
         *     "algorithm": "Argon2id",
         *     "iterations": 100_000,
         *     "keyLength": 32,
         *     "memoryKb": 65536,
         *     "parallelism": 4
         *   }
         * }
         */
        [HttpPost]
        public async Task<ApiResponse<PublicUser>> RegisterUser([FromBody] UserRegistrationReq req)
        {
            PublicUser user = await _authService.RegisterUserAsync(req);
            return ApiResponse<PublicUser>.Created(user);
        }
    }
}
