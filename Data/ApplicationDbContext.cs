using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using ZoraVault.Models.Entities;
using ZoraVault.Models.Internal;

namespace ZoraVault.Data
{
    /// <summary>
    /// ApplicationDbContext represents the database context for the ZoraVault application.
    /// It contains all DbSet properties representing tables and configures entity relationships
    /// and conversions for complex types.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructor for ApplicationDbContext, receiving DbContextOptions from DI.
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // ---------------------------------------------------------------------------
        // DbSet properties (tables)
        // ---------------------------------------------------------------------------
        public DbSet<User> Users { get; set; }
        public DbSet<VaultItem> VaultItems { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }

        // ---------------------------------------------------------------------------
        // Model configuration
        // ---------------------------------------------------------------------------
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Conversion for KdfParamsDTO:
            // The KdfParams property of User is a complex type.
            // EF Core cannot map it directly, so we serialize it to JSON for storage.
            var converter = new ValueConverter<KdfParams, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<KdfParams>(v, (JsonSerializerOptions?)null)!
            );

            modelBuilder.Entity<User>()
                .Property(u => u.KdfParams)
                .HasConversion(converter);

            // Composite primary key for UserDevice:
            // Since a user can have multiple devices and a device can belong to multiple users,
            // we define a composite primary key on UserId + DeviceId
            modelBuilder.Entity<UserDevice>()
                .HasKey(ud => new { ud.UserId, ud.DeviceId });

            // Relationships for UserSettings
            modelBuilder.Entity<UserSettings>(entity =>
            {
                // One User has many UserSettings
                entity.HasOne(us => us.User)
                    .WithMany(u => u.UserSettings)
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // One Device has many UserSettings
                entity.HasOne(us => us.Device)
                    .WithMany(d => d.UserSettings)
                    .HasForeignKey(us => us.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Ensure each (User, Device) pair has only one settings entry
                entity.HasIndex(us => new { us.UserId, us.DeviceId })
                      .IsUnique();
            });
        }
    }
}
