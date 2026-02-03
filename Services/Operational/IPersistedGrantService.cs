namespace IdentityServerHost.Services.Operational;

public interface IPersistedGrantService
{
    Task<(IEnumerable<IdentityServer4.EntityFramework.Entities.PersistedGrant> Items, int Total)> GetPagedAsync(int page, int pageSize, string? subjectId, string? clientId, string? type);
    Task<bool> RevokeAsync(string key);
    Task<int> RevokeBySubjectClientAsync(string subjectId, string clientId);
}
