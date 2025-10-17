using Microsoft.EntityFrameworkCore;
using ZoraVault.Data;
using ZoraVault.Models.Entities;

namespace ZoraVault.Helpers
{
    public class DeviceHelpers
    {
        public async static Task<bool> IsDeviceRegistered(ApplicationDbContext db, string publicKey)
        {
            //Device handling
            string fingerprint = SecurityHelpers.ComputeSHA256HashHex(publicKey);
            Device? device = await db.Devices.FirstOrDefaultAsync(d => d.Fingerprint == fingerprint);

            return device != null;
        }
    }
}
