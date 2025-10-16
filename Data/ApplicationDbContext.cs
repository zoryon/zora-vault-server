using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

using ZoraVault.Models.DTOs;
using ZoraVault.Models.Entities;

namespace ZoraVault.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Properties representing database tables
        public DbSet<User> Users { get; set; }
        public DbSet<VaultItem> VaultItems { get; set; }
        public DbSet<Passkey> Passkeys { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var converter = new ValueConverter<KdfParams, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<KdfParams>(v, (JsonSerializerOptions?)null)!
            );

            modelBuilder.Entity<User>()
                .Property(u => u.KdfParams)
                .HasConversion(converter);
        }
    }
}
