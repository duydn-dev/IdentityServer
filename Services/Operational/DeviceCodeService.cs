using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Services.Operational;

public class DeviceCodeService : IDeviceCodeService
{
    private readonly PersistedGrantDbContext _db;

    public DeviceCodeService(PersistedGrantDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<DeviceFlowCodes> Items, int Total)> GetPagedAsync(int page, int pageSize, string? userCode, string? clientId)
    {
        var query = _db.DeviceFlowCodes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(userCode))
            query = query.Where(d => d.UserCode != null && d.UserCode.Contains(userCode));
        if (!string.IsNullOrWhiteSpace(clientId))
            query = query.Where(d => d.ClientId != null && d.ClientId.Contains(clientId));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.CreationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> RemoveAsync(string userCode)
    {
        var device = await _db.DeviceFlowCodes.FindAsync(userCode);
        if (device == null) return false;
        _db.DeviceFlowCodes.Remove(device);
        await _db.SaveChangesAsync();
        return true;
    }
}
