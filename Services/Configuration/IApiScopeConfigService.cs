namespace IdentityServerHost.Services.Configuration;

public interface IApiScopeConfigService
{
    Task<IEnumerable<IdentityServer4.EntityFramework.Entities.ApiScope>> GetAllAsync(int page, int pageSize, string? search);
    Task<int> GetCountAsync(string? search);
    Task<IdentityServer4.EntityFramework.Entities.ApiScope?> GetByIdAsync(int id);
    Task<IdentityServer4.EntityFramework.Entities.ApiScope?> GetByNameAsync(string name);
    Task<bool> CreateAsync(IdentityServer4.EntityFramework.Entities.ApiScope scope);
    Task<bool> UpdateAsync(IdentityServer4.EntityFramework.Entities.ApiScope scope);
    Task<bool> DeleteAsync(int id);
}
