using System.ComponentModel.DataAnnotations;

namespace ZoraVault.Models.Entities
{
    public class Session
    {
        public Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required Guid DeviceId { get; set; }

        [MaxLength(512)]
        public required string RefreshToken { get; set; }

        [MaxLength(45)]
        public required string IpAddress { get; set; }

        [MaxLength(1024)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
        public Device Device { get; set; } = null!;
    }
}
