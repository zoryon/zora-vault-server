namespace ZoraVault.Models.DTOs
{
    public class VerifyDeviceResDTO
    {
        public required bool Verified { get; set; }
        public required Guid DeviceId { get; set; }
        public required Guid UserId { get; set; }

    }
}
