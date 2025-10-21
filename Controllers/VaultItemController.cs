using Microsoft.AspNetCore.Mvc;
using ZoraVault.Extensions;
using ZoraVault.Models.DTOs.VaultItems;
using ZoraVault.Models.Entities;
using ZoraVault.Models.Internal;
using ZoraVault.Models.Internal.Common;
using ZoraVault.Services;

namespace ZoraVault.Controllers
{
    /// <summary>
    /// The VaultItemController handles all API endpoints related to a user's vault items.
    /// Responsibilities include:
    /// - Creating new vault items
    /// - Retrieving vault items (single or list)
    /// - Updating existing vault items
    /// - Deleting vault items
    /// </summary>
    [ApiController]
    public class VaultItemController : ControllerBase
    {
        private readonly VaultItemService _vaultItemService;    // Handles business logic for vault items

        /// <summary>
        /// Constructor-based dependency injection for the VaultItemService.
        /// </summary>
        /// <param name="vaultItemService">Service that performs operations on vault items.</param>
        public VaultItemController(VaultItemService vaultItemService)
        {
            _vaultItemService = vaultItemService;
        }

        // ---------------------------------------------------------------------------
        // GET /api/users/me/vault-items
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Retrieves a list of vault items for the authenticated user.
        /// If no items exist, returns an empty list (not null).
        /// </summary>
        [HttpGet("/api/users/me/vault-items")]
        public async Task<ApiResponse<List<PublicVaultItem>>> GetVaultItemsAsync()
        {
            AuthContext ctx = HttpContext.GetAuthContext();

            // Fetch all vault items for the user and convert them to public objects
            List<PublicVaultItem> items =  await _vaultItemService.FetchVaultItemsAsync(ctx.UserId);

            return ApiResponse<List<PublicVaultItem>>.SuccessResponse(items);
        }

        // ---------------------------------------------------------------------------
        // GET /api/users/me/vault-item/{vaultItemId}
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Retrieves a single vault item by its ID for the authenticated user.
        /// Throws KeyNotFoundException if the item does not exist.
        /// </summary>
        [HttpGet("/api/users/me/vault-item/{vaultItemId}")]
        public async Task<ApiResponse<PublicVaultItem>> GetVaultItemAsync([FromRoute] Guid vaultItemId)
        {
            AuthContext ctx = HttpContext.GetAuthContext();

            // Fetch the single vault item as a public object
            PublicVaultItem vi = await _vaultItemService.FetchVaultItemAsync(ctx.UserId, vaultItemId);

            return ApiResponse<PublicVaultItem>.SuccessResponse(vi);
        }

        // ---------------------------------------------------------------------------
        // POST /api/users/me/vault-items
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Creates a new vault item for the authenticated user.
        /// EncryptedData is expected to be encrypted client-side.
        /// </summary>
        /// <param name="req">The request DTO containing vault item data to create.</param>
        /// <returns>The created vault item wrapped in a standardized API response with HTTP 201 status.</returns>
        [HttpPost("/api/users/me/vault-items")]
        public async Task<ApiResponse<PublicVaultItem>> CreateVaultItemAsync([FromBody] CreateVaultItemRequest req)
        {
            AuthContext ctx = HttpContext.GetAuthContext();

            const int MAX_ENCRYPTED_SIZE = 50000;   // 50 KB max
            if (req.EncryptedData == null || req.EncryptedData.Length <= 0)
                throw new ArgumentException("EncryptedData cannot be empty.");

            if (req.EncryptedData.Length > MAX_ENCRYPTED_SIZE)
                throw new ArgumentException($"EncryptedData exceeds the maximum allowed size of {MAX_ENCRYPTED_SIZE} bytes (50 KB)");

            PublicVaultItem vi = await _vaultItemService.AddVaultItemAsync(ctx.UserId, req);

            return ApiResponse<PublicVaultItem>.Created(vi);
        }

        // ---------------------------------------------------------------------------
        // PUT /api/users/me/vault-item/{vaultItemId}
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Updates an existing vault item for the authenticated user.
        /// Fully replaces the EncryptedData and Type; the item must exist.
        /// </summary>
        [HttpPut("/api/users/me/vault-item/{vaultItemId}")]
        public async Task<ApiResponse<PublicVaultItem>> UpdateVaultItemAsync([FromRoute] Guid vaultItemId, [FromBody] UpdateVaultItemRequest req)
        {
            AuthContext ctx = HttpContext.GetAuthContext();

            const int MAX_ENCRYPTED_SIZE = 50000;
            if (req.EncryptedData == null || req.EncryptedData.Length <= 0)
                throw new ArgumentException("EncryptedData cannot be empty.");

            if (req.EncryptedData.Length > MAX_ENCRYPTED_SIZE)
                throw new ArgumentException($"EncryptedData exceeds the maximum allowed size of {MAX_ENCRYPTED_SIZE} bytes (50 KB)");

            PublicVaultItem vi = await _vaultItemService.ReplaceVaultItemAsync(ctx.UserId, vaultItemId, req);

            return ApiResponse<PublicVaultItem>.SuccessResponse(vi, 200, "Vault item was updated successfully");
        }

        // ---------------------------------------------------------------------------
        // DELETE /api/users/me/vault-item/{vaultItemId}
        // ---------------------------------------------------------------------------
        /// <summary>
        /// Deletes a vault item for the authenticated user.
        /// Returns only the ID of the deleted item for confirmation.
        /// </summary>
        [HttpDelete("/api/users/me/vault-item/{vaultItemId}")]
        public async Task<ApiResponse<Guid>> DeleteVaultItemAsync([FromRoute] Guid vaultItemId)
        {
            AuthContext ctx = HttpContext.GetAuthContext();

            Guid removedId = await _vaultItemService.RemoveVaultItemAsync(ctx.UserId, vaultItemId);

            return ApiResponse<Guid>.SuccessResponse(removedId, 200, "Vault item was removed successfully");
        }
    }
}
