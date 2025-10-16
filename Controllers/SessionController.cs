using Microsoft.AspNetCore.Mvc;
using ZoraVault.Models.Common;
using ZoraVault.Models.DTOs;
using ZoraVault.Services;

namespace ZoraVault.Controllers
{
    [ApiController]
    [Route("/api/sessions")]
    public class SessionController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly DeviceService _deviceService;

        public SessionController(AuthService authService, DeviceService deviceService)
        {
            _authService = authService;
            _deviceService = deviceService;
        }

        [HttpPost]
        public async Task<ApiResponse<DeviceChallengeDTO>> AuthenticateUserAsync([FromBody] UserLoginReqDTO req)
        {
            // This function only initiates the authentication and returns the challenge to the client
            DeviceChallengeDTO challenge = await _authService.AuthenticateUserAsync(req);
            return ApiResponse<DeviceChallengeDTO>.SuccessResponse(challenge, 200, "Login successful, now must verify the device through the challenge");
        }

        [HttpPost("/proofs")]
        public async Task<ApiResponse<bool>> VerifyDevice([FromBody] VerifyDeviceReqDTO req)
        {
            // This function verifies the device and creates a session if successful
            VerifyDeviceResDTO result = await _deviceService.VerifyChallengeAsync(req);
            
            CreateSessionResDTO tokens = await _authService.CreateSessionAsync(new CreateSessionReqDTO(result.UserId, result.DeviceId));

            return ApiResponse<bool>.SuccessResponse(true, 200, "Device verified successfully");
        }
    }
}
