using IdentityServerHost.Data;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Http;

namespace IdentityServerHost.Services.Audit;

public class AuditService : IAuditService
{
    private readonly AuditDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(AuditDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null, string? details = null, bool success = true)
    {
        var ctx = _httpContextAccessor.HttpContext;
        var userId = ctx?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = ctx?.User?.Identity?.Name;

        var log = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ctx?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = ctx?.Request?.Headers.UserAgent.ToString(),
            Success = success
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
