namespace ZoraVault.Models.Internal
{
    public class Challenge
    {
        public required Guid DeviceId { get; set; }
        public required Guid UserId { get; set; }
        public required string Random { get; set; } = null!;
    }
}
