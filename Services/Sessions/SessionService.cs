using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Services.Sessions;

public class SessionService : ISessionService
{
    private readonly PersistedGrantDbContext _db;

    public SessionService(PersistedGrantDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SessionInfo>> GetUserSessionsAsync(string userId)
    {
        var grants = await _db.PersistedGrants
            .Where(g => g.SubjectId == userId)
            .OrderByDescending(g => g.CreationTime)
            .Select(g => new SessionInfo
            {
                SessionId = g.SessionId ?? g.Key,
                ClientId = g.ClientId,
                SubjectId = g.SubjectId,
                CreationTime = g.CreationTime,
                Expiration = g.Expiration,
                Type = g.Type
            })
            .ToListAsync();

        return grants.GroupBy(g => g.SessionId).Select(g => g.First());
    }

    public async Task<bool> RevokeSessionAsync(string sessionId)
    {
        var grants = await _db.PersistedGrants.Where(g => g.SessionId == sessionId).ToListAsync();
        _db.PersistedGrants.RemoveRange(grants);
        await _db.SaveChangesAsync();
        return grants.Count > 0;
    }

    public async Task<int> RevokeAllUserSessionsAsync(string userId)
    {
        var grants = await _db.PersistedGrants.Where(g => g.SubjectId == userId).ToListAsync();
        _db.PersistedGrants.RemoveRange(grants);
        await _db.SaveChangesAsync();
        return grants.Count;
    }
}
