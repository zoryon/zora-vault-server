using Microsoft.EntityFrameworkCore;
using ZoraVault.Data;
using ZoraVault.Models.DTOs.Users;
using ZoraVault.Models.Entities;
using ZoraVault.Models.Internal;
using ZoraVault.Models.Internal.Enum;

namespace ZoraVault.Services
{
    /// <summary>
    /// UserService handles operations related to user profile and user-specific settings:
    /// - Fetching user public data
    /// - Updating user fields (username, vault blob)
    /// - Updating device-specific user settings
    /// </summary>
    public class UserService
    {
        private readonly ApplicationDbContext _db;

        public UserService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retrieves the public information of the current user.
        /// </summary>
        /// <param name="userId">The ID of the user whose data is requested.</param>
        /// <returns>A <see cref="PublicUser"/> containing only non-sensitive user data.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the user does not exist.</exception>
        public async Task<PublicUser> FetchCurrentUserAsync(Guid userId)
        {
            User? user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("User was not found");

            return new PublicUser(user);
        }

        /// <summary>
        /// Updates core fields of the current user.
        /// Only username and encrypted vault blob are modified; other fields remain intact.
        /// </summary>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="req">Contains the new values for username and vault blob.</param>
        /// <returns>The updated <see cref="PublicUser"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the user does not exist.</exception>
        public async Task<PublicUser> ReplaceFieldsCurrentUserAsync(Guid userId, PatchUserRequest req)
        {
            User? user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException("User was not found");

            // Update only allowed fields
            _db.Entry(user).CurrentValues.SetValues(req);

            await _db.SaveChangesAsync();

            return new PublicUser(user);
        }

        /// <summary>
        /// Updates device-specific settings for the current user.
        /// All user settings for the specified device are replaced with the provided values.
        /// </summary>
        /// <param name="userId">The ID of the user whose settings are being updated.</param>
        /// <param name="deviceId">The ID of the device for which settings are updated.</param>
        /// <param name="req">Contains the new user settings.</param>
        /// <returns>A <see cref="UpdateUserSettingsResponse"/> representing the updated settings.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if user settings for the specified device do not exist.</exception>
        public async Task<UpdateUserSettingsResponse> ReplaceCurrentUserSettingsAsync(Guid userId, Guid deviceId, UpdateUserSettingsRequest req)
        {
            UserSettings? us = await _db.UserSettings
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.DeviceId == deviceId);
            if (us == null)
                throw new KeyNotFoundException("User settings for specified device were not found");

            _db.Entry(us).CurrentValues.SetValues(req);

            await _db.SaveChangesAsync();

            return new UpdateUserSettingsResponse(us);
        }

        /// <summary>
        /// Permanently removes the specified user account and all related data (sessions, devices, vault items, etc.).
        /// </summary>
        /// <param name="userId">The unique ID of the user to remove.</param>
        /// <returns>The ID of the deleted user.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the user does not exist.</exception>
        public async Task<Guid> RemoveCurrentUserAccountAsync(Guid userId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();

            int deletedUserRecord = await _db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();
            if (deletedUserRecord != 1)
                throw new KeyNotFoundException("User was not found");

            await _db.Sessions.Where(s => s.UserId == userId).ExecuteDeleteAsync();
            await _db.UserDevices.Where(ud => ud.UserId == userId).ExecuteDeleteAsync();
            await _db.VaultItems.Where(vi => vi.UserId == userId).ExecuteDeleteAsync();
            await _db.UserSettings.Where(us => us.UserId == userId).ExecuteDeleteAsync();

            await tx.CommitAsync();

            return userId;
        }
    }
}
