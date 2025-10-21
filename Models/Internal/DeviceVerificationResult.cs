namespace ZoraVault.Models.Internal
{
    public class DeviceVerificationResult
    {
        public required bool IsVerified { get; set; }
        public required Guid DeviceId { get; set; }
        public required Guid UserId { get; set; }
    }
}
