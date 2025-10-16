using Microsoft.AspNetCore.Mvc;
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

        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationReq req)
        {
            await _authService.RegisterUserAsync(req);
            return Ok(new { Message = "User info endpoint" });
        }
    }
}
