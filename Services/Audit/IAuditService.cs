namespace IdentityServerHost.Services.Audit;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null, string? details = null, bool success = true);
}
