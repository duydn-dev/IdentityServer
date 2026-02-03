namespace IdentityServerHost.Services.Configuration;

public interface IApiResourceConfigService
{
    Task<IEnumerable<IdentityServer4.EntityFramework.Entities.ApiResource>> GetAllAsync(int page, int pageSize, string? search);
    Task<int> GetCountAsync(string? search);
    Task<IdentityServer4.EntityFramework.Entities.ApiResource?> GetByIdAsync(int id);
    Task<IdentityServer4.EntityFramework.Entities.ApiResource?> GetByNameAsync(string name);
    Task<bool> CreateAsync(IdentityServer4.EntityFramework.Entities.ApiResource resource);
    Task<bool> UpdateAsync(IdentityServer4.EntityFramework.Entities.ApiResource resource);
    Task<bool> DeleteAsync(int id);
}
