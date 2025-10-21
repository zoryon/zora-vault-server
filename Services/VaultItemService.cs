using Microsoft.EntityFrameworkCore;
using ZoraVault.Data;
using ZoraVault.Models.DTOs.VaultItems;
using ZoraVault.Models.Entities;
using ZoraVault.Models.Internal;

namespace ZoraVault.Services
{
    /// <summary>
    /// The VaultItemService handles the business logic and data persistence 
    /// related to vault items. 
    /// 
    /// Responsibilities:
    /// - Creating and saving encrypted vault items for users
    /// - Retrieving, updating, or deleting vault items
    /// </summary>
    public class VaultItemService
    {
        private readonly ApplicationDbContext _db;

        /// <summary>
        /// Constructor-based dependency injection for the application's database context.
        /// </summary>
        /// <param name="db">Injected instance of ApplicationDbContext used for database operations.</param>
        public VaultItemService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<PublicVaultItem>> FetchVaultItemsAsync(Guid userId)
        {
            List<VaultItem> items = await _db.VaultItems
                .AsNoTracking() // Read-only operation
                .Where(vi => vi.UserId == userId)
                .OrderByDescending(vi => vi.UpdatedAt)  // Newest first
                .ToListAsync();

            // Convert VaultItem -> PublicVaultItem
            return [.. items.Select(vi => new PublicVaultItem(vi))];
        }

        public async Task<PublicVaultItem> FetchVaultItemAsync(Guid userId, Guid vaultItemId)
        {
            VaultItem? vi = await _db.VaultItems
                .AsNoTracking() // Read-only operation
                .FirstOrDefaultAsync(vi => vi.UserId == userId && vi.Id == vaultItemId);

            if (vi == null)
                throw new KeyNotFoundException("Vault item not found");

            return new PublicVaultItem(vi);
        }

        /// <summary>
        /// Creates and persists a new vault item for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user to whom the vault item belongs.</param>
        /// <param name="req">DTO containing all necessary information for the vault item creation.</param>
        /// <returns>The created <see cref="VaultItem"/> entity after it has been saved to the database.</returns>
        public async Task<PublicVaultItem> AddVaultItemAsync(Guid userId, CreateVaultItemRequest req)
        {
            // EncryptedData Field is expected to be encrypted client-side BEFORE reaching the backend
            var entry = _db.VaultItems.Add(new VaultItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = req.Type,
                EncryptedData = req.EncryptedData,  // Encrypted payload containing sensitive information (maximum: mb)
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Return the saved entity
            return new PublicVaultItem(entry.Entity);
        }

        public async Task<PublicVaultItem> ReplaceVaultItemAsync(Guid userId, Guid vaultItemId, UpdateVaultItemRequest req)
        {
            // EncryptedData Field is expected to be encrypted client-side BEFORE reaching the backend
            VaultItem? existingItem = await _db.VaultItems
                .FirstOrDefaultAsync(vi => vi.UserId == userId && vi.Id == vaultItemId);
            if (existingItem == null)
                throw new KeyNotFoundException("Vault item not found");

            // Replace the encrypted data and update the type (if needed)
            existingItem.EncryptedData = req.EncryptedData;
            existingItem.Type = req.Type;
            existingItem.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Return the replaced entity
            return new PublicVaultItem(existingItem);
        }

        public async Task<Guid> RemoveVaultItemAsync(Guid userId, Guid vaultItemId)
        {
            VaultItem? existingItem = await _db.VaultItems
                .FirstOrDefaultAsync(vi => vi.UserId == userId && vi.Id == vaultItemId);
            if (existingItem == null)
                throw new KeyNotFoundException("Vault item not found");

            _db.VaultItems.Remove(existingItem);
            await _db.SaveChangesAsync();

            return existingItem.Id;
        }
    }
}
