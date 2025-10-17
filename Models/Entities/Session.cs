namespace ZoraVault.Models.Entities
{
    public class Session
    {
        public Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required Guid DeviceId { get; set; }

        public required string RefreshToken { get; set; }
        public required string IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
        public Device Device { get; set; } = null!;
    }
}
