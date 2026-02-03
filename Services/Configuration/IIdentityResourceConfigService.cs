namespace IdentityServerHost.Services.Configuration;

public interface IIdentityResourceConfigService
{
    Task<IEnumerable<IdentityServer4.EntityFramework.Entities.IdentityResource>> GetAllAsync(int page, int pageSize, string? search);
    Task<int> GetCountAsync(string? search);
    Task<IdentityServer4.EntityFramework.Entities.IdentityResource?> GetByIdAsync(int id);
    Task<IdentityServer4.EntityFramework.Entities.IdentityResource?> GetByNameAsync(string name);
    Task<bool> CreateAsync(IdentityServer4.EntityFramework.Entities.IdentityResource resource);
    Task<bool> UpdateAsync(IdentityServer4.EntityFramework.Entities.IdentityResource resource);
    Task<bool> DeleteAsync(int id);
}
