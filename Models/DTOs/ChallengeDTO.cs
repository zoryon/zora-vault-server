namespace ZoraVault.Models.DTOs
{
    public class ChallengeDTO
    {
        public required Guid DeviceId { get; set; }
        public required Guid UserId { get; set; }
        public required string Random { get; set; } = null!;
    }
}
