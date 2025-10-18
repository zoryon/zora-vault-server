using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ZoraVault.Helpers;
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

        // POST /api/sessions
        // This function verifies the device and creates a session if successful
        [HttpPost]
        public async Task<ApiResponse<CreateSessionResDTO>> LoginAuthenticatedUserAsync([FromBody] CreateSessionReqDTO req)
        {
            VerifyDeviceResDTO result = await _deviceService.VerifyChallengeAsync(req);
            if (!result.IsVerified)
                throw new UnauthorizedAccessException("Device verification failed");

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            CreateSessionResDTO tokens = await _authService.CreateSessionAsync(result.UserId, result.DeviceId, ipAddress);

            return ApiResponse<CreateSessionResDTO>.SuccessResponse(tokens, 200, "Authorized");
        }

        // POST /api/sessions/credentials
        [HttpPost("credentials")]
        public async Task<ApiResponse<string>> AuthAndIssueApiTokenAsync([FromBody] UserAuthReqDTO req)
        {
            // Authenticates the user credentials
            PublicUserDTO user = await _authService.AuthenticateUserAsync(req)
                ?? throw new UnauthorizedAccessException("Invalid credentials");

            // Send the client a token to call the api dedicated to issue challenges for authenticating devices
            string challengesApiToken = _authService.GrantChallengesAPIAccess(user.Id);

            return ApiResponse<string>.SuccessResponse(challengesApiToken, 200, "Authentication successful, now must verify the device in order to login");
        }

        // POST /api/sessions/challenges
        [HttpPost("challenges")]
        public async Task<ApiResponse<DeviceChallengeApiResDTO>> IssueDeviceChallengeAsync([FromBody] DeviceChallengeApiReqDTO req)
        {
            // Verify the API token and get the user ID
            Guid userId = _authService.VerifyDeviceChallengeAccessTokenAsync(req.AccessApiToken);

            // Find or register the device
            PublicDevice device = await _deviceService.FindOrRegisterDeviceAsync(userId, req.PublicKey);

            // Generate a random challenge
            ChallengeDTO challengeObject = new()
            {
                DeviceId = device.Id,
                UserId = userId,
                Random = SecurityHelpers.GenerateRandomBase64String(32)
            };
            string plainChallenge = JsonSerializer.Serialize(challengeObject);

            // Create and send the encrypted challenge to the device
            bool success = await _deviceService.SaveTempChallengeAsync(device, plainChallenge);
            if (!success)
                throw new Exception("Failed to save temporary challenge, please retry later");

            return ApiResponse<DeviceChallengeApiResDTO>.SuccessResponse(new DeviceChallengeApiResDTO
            {
                EncryptedChallenge = SecurityHelpers.EncryptWithPublicKey(plainChallenge, req.PublicKey),
                AccessApiToken = _authService.GrantSessionAPIAccess(userId, device.Id)
            }, 200, "Challenge issued correctly");
        }
    }
}
