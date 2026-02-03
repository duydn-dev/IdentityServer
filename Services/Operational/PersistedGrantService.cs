using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Services.Operational;

public class PersistedGrantService : IPersistedGrantService
{
    private readonly PersistedGrantDbContext _db;

    public PersistedGrantService(PersistedGrantDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<IdentityServer4.EntityFramework.Entities.PersistedGrant> Items, int Total)> GetPagedAsync(int page, int pageSize, string? subjectId, string? clientId, string? type)
    {
        var query = _db.PersistedGrants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(subjectId))
            query = query.Where(g => g.SubjectId != null && g.SubjectId.Contains(subjectId));
        if (!string.IsNullOrWhiteSpace(clientId))
            query = query.Where(g => g.ClientId != null && g.ClientId.Contains(clientId));
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(g => g.Type != null && g.Type.Contains(type));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(g => g.CreationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> RevokeAsync(string key)
    {
        var grant = await _db.PersistedGrants.FindAsync(key);
        if (grant == null) return false;
        _db.PersistedGrants.Remove(grant);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> RevokeBySubjectClientAsync(string subjectId, string clientId)
    {
        var grants = await _db.PersistedGrants
            .Where(g => g.SubjectId == subjectId && g.ClientId == clientId)
            .ToListAsync();
        _db.PersistedGrants.RemoveRange(grants);
        await _db.SaveChangesAsync();
        return grants.Count;
    }
}
