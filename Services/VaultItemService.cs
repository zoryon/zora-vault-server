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

        /// <summary>
        /// Retrieves all non-deleted vault items for the specified user.
        /// Items are automatically filtered through the global query filter 
        /// (<c>vi => vi.DeletedAt == null</c>) and ordered by <see cref="VaultItem.UpdatedAt"/> (newest first).
        /// </summary>
        /// <param name="userId">The ID of the user whose vault items will be fetched.</param>
        /// <returns>A list of <see cref="PublicVaultItem"/> objects representing the user's active vault items.</returns>
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

        /// <summary>
        /// Retrieves a single vault item for the specified user by its unique identifier.
        /// Returns only non-deleted items (global filter applied).
        /// </summary>
        /// <param name="userId">The ID of the user who owns the vault item.</param>
        /// <param name="vaultItemId">The unique identifier of the vault item.</param>
        /// <returns>A <see cref="PublicVaultItem"/> representing the requested vault item.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the vault item cannot be found.</exception>
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
        /// Retrieves all soft-deleted vault items (trash) for the specified user.
        /// This method explicitly ignores the global query filter to include deleted entries.
        /// </summary>
        /// <param name="userId">The ID of the user whose deleted vault items will be fetched.</param>
        /// <returns>A list of <see cref="PublicVaultItem"/> objects representing soft-deleted vault items.</returns>
        public async Task<List<PublicVaultItem>> FetchSoftRemovedVaultItemAsync(Guid userId)
        {
            List<VaultItem> items = await _db.VaultItems
                .AsNoTracking() // Read-only operation
                .IgnoreQueryFilters() // Include soft-deleted items in the query
                .Where(vi => vi.UserId == userId && vi.DeletedAt != null)
                .OrderByDescending(vi => vi.UpdatedAt)  // Newest first
                .ToListAsync();

            // Convert VaultItem -> PublicVaultItem
            return [.. items.Select(vi => new PublicVaultItem(vi))];
        }

        /// <summary>
        /// Creates and persists a new vault item for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user to whom the vault item belongs to.</param>
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

        /// <summary>
        /// Updates all the fields of a vault item for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user to whom the vault item belongs to.</param>
        /// <param name="vaultItemId">The ID of the vault item.</param>
        /// <param name="req">DTO containing all necessary information for the vault item update.</param>
        /// <returns>The updated <see cref="PublicVaultItem"/> public entity after it has been saved to the database.</returns>
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

        /// <summary>
        /// Soft delete a vault item for the specified user.
        /// Soft delete: (The SQL database record is not removed, but marked as deleted via the DeletedAt timestamp).
        /// The SQL database will clean up soft-deleted records via a scheduled job after 30 days.
        /// </summary>
        /// <param name="userId">The ID of the user to whom the vault item belongs to.</param>
        /// <param name="vaultItemId">The ID of the vault item.</param>
        /// <returns>The created <see cref="Guid"/>Id of the soft deleted vault item.</returns>
        public async Task<Guid> SoftRemoveVaultItemAsync(Guid userId, Guid vaultItemId)
        {
            VaultItem? existingItem = await _db.VaultItems
                .FirstOrDefaultAsync(vi => vi.UserId == userId && vi.Id == vaultItemId);
            if (existingItem == null)
                throw new KeyNotFoundException("Vault item not found");

            // Soft delete by setting DeletedAt timestamp
            existingItem.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return existingItem.Id;
        }

        /// <summary>
        /// Restore a soft deleted vault item for the specified user.
        /// Soft delete: (The SQL database record is not removed, but marked as deleted via the DeletedAt timestamp).
        /// </summary>
        /// <param name="userId">The ID of the user to whom the vault item belongs to.</param>
        /// <param name="vaultItemId">The ID of the soft deleted vault item.</param>
        /// <returns>The created <see cref="PublicVaultItem"/>Public version of a vault item, used for safety.</returns>
        public async Task<PublicVaultItem> RestoreSoftRemovedVaultItemAsync(Guid userId, Guid vaultItemId)
        {
            VaultItem? existingItem = await _db.VaultItems
                .IgnoreQueryFilters() // Include soft-deleted items in the query
                .FirstOrDefaultAsync(vi => vi.UserId == userId && vi.Id == vaultItemId);
            if (existingItem == null)
                throw new KeyNotFoundException("Vault item not found");

            // Restore by unsetting DeletedAt timestamp
            existingItem.DeletedAt = null;

            await _db.SaveChangesAsync();

            return new PublicVaultItem(existingItem);
        }
    }
}
