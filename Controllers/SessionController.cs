using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ZoraVault.Helpers;
using ZoraVault.Models.Common;
using ZoraVault.Models.DTOs;
using ZoraVault.Services;

namespace ZoraVault.Controllers
{
    /// <summary>
    /// The SessionController handles all API endpoints related to user sessions and authentication flows.
    /// This includes:
    /// - Logging in authenticated users
    /// - Issuing device challenges
    /// - Exchanging credentials for API tokens
    /// - Refreshing access tokens
    /// </summary>
    [ApiController]
    [Route("/api/sessions")]
    public class SessionController : ControllerBase
    {
        private readonly AuthService _authService;      // Handles authentication, token issuance, and session management
        private readonly DeviceService _deviceService;  // Handles device registration, challenge, and verification

        // Dependency injection: both services are injected via constructor
        public SessionController(AuthService authService, DeviceService deviceService)
        {
            _authService = authService;
            _deviceService = deviceService;
        }

        // ---------------------------------------------------------------------------
        // POST /api/sessions
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Verifies a device challenge response and creates a new session for the user if verification succeeds.
        /// </summary>
        [HttpPost]
        public async Task<ApiResponse<CreateSessionResDTO>> LoginAuthenticatedUserAsync([FromBody] CreateSessionReqDTO req)
        {
            // Verify the challenge response provided by the device.
            // This ensures the device actually owns the correct private key.
            VerifyDeviceResDTO result = await _deviceService.VerifyChallengeAsync(req);
            if (!result.IsVerified)
                throw new UnauthorizedAccessException("Device verification failed");

            // Capture the IP address of the incoming request (for session tracking & security)
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Create a new session or update existing one and generate access/refresh tokens
            CreateSessionResDTO tokens = await _authService.CreateSessionAsync(result.UserId, result.DeviceId, ipAddress);

            // Return tokens to the client
            return ApiResponse<CreateSessionResDTO>.SuccessResponse(tokens, 200, "Authorized");
        }

        // ---------------------------------------------------------------------------
        // POST /api/sessions/credentials
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Authenticates a user by credentials (username/email + password hash)
        /// and returns a temporary API token that allows device verification.
        /// </summary>
        [HttpPost("credentials")]
        public async Task<ApiResponse<string>> AuthAndIssueApiTokenAsync([FromBody] UserAuthReqDTO req)
        {
            // Authenticate the user
            PublicUserDTO user = await _authService.AuthenticateUserAsync(req)
                ?? throw new UnauthorizedAccessException("Invalid credentials");

            // Grant a short-lived API token (2 min) used to call the challenge endpoints.
            // This token confirms that the user is legitimate but device still must verify.
            string challengesApiToken = _authService.GrantChallengesAPIAccess(user.Id);

            // Return the token to the client with instructions to continue verification
            return ApiResponse<string>.SuccessResponse(
                challengesApiToken, 
                200, 
                "Authentication successful, now must verify the device in order to login"
            );
        }

        // ---------------------------------------------------------------------------
        // POST /api/sessions/challenges
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Issues a cryptographic challenge to a device after the user authenticates.
        /// The challenge must later be decrypted and answered by the device using its private key.
        /// </summary>
        [HttpPost("challenges")]
        public async Task<ApiResponse<DeviceChallengeApiResDTO>> IssueDeviceChallengeAsync([FromBody] DeviceChallengeApiReqDTO req)
        {
            // Verify the API token (the one from /credentials) and get the user ID
            Guid userId = _authService.VerifyDeviceChallengeAccessTokenAsync(req.AccessApiToken);

            // Find the device by its public key or register it as a new one
            PublicDevice device = await _deviceService.FindOrRegisterDeviceAsync(req.PublicKey);

            // Construct a cryptographic challenge object
            // It includes device ID, user ID, and a random string for uniqueness
            ChallengeDTO challengeObject = new()
            {
                DeviceId = device.Id,
                UserId = userId,
                Random = SecurityHelpers.GenerateRandomBase64String(32)
            };

            // Serialize the challenge into plaintext JSON before encryption
            string plainChallenge = JsonSerializer.Serialize(challengeObject);

            // Save the temporary challenge server-side (to verify later)
            bool success = await _deviceService.SaveTempChallengeAsync(device, plainChallenge);
            if (!success)
                throw new Exception("Failed to save temporary challenge, please retry later");

            // Encrypt the plaintext challenge with the device's public key,
            // so only that device (with its private key) can decrypt and respond.
            return ApiResponse<DeviceChallengeApiResDTO>.SuccessResponse(new DeviceChallengeApiResDTO
            {
                EncryptedChallenge = SecurityHelpers.EncryptWithPublicKey(plainChallenge, req.PublicKey),
                AccessApiToken = _authService.GrantSessionAPIAccess(userId, device.Id)
            }, 200, "Challenge issued correctly");
        }

        // ---------------------------------------------------------------------------
        // POST /api/sessions/tokens/refresh-tokens
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Allows clients to refresh an expired access token using a valid refresh token.
        /// </summary>
        [HttpPost("tokens/refresh-tokens")]
        public async Task<ApiResponse<string>> RefreshAccessTokenAsync([FromBody] string refreshToken)
        {
            return ApiResponse<string>.SuccessResponse(
                await _authService.RefreshAccessTokenAsync(refreshToken), // Validate refresh token and issue a new access token
                200,
                "Access token refreshed successfully"
            );
        }
    }
}
