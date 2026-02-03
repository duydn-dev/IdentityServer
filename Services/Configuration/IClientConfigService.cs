namespace IdentityServerHost.Services.Configuration;

public interface IClientConfigService
{
    Task<IEnumerable<IdentityServer4.EntityFramework.Entities.Client>> GetAllAsync(int page, int pageSize, string? search);
    Task<int> GetCountAsync(string? search);
    Task<IdentityServer4.EntityFramework.Entities.Client?> GetByIdAsync(int id);
    Task<IdentityServer4.EntityFramework.Entities.Client?> GetByClientIdAsync(string clientId);
    Task<bool> CreateAsync(IdentityServer4.EntityFramework.Entities.Client client);
    Task<bool> UpdateAsync(IdentityServer4.EntityFramework.Entities.Client client);
    Task<bool> DeleteAsync(int id);
}
