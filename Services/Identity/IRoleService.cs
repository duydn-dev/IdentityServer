namespace IdentityServerHost.Services.Identity;

public interface IRoleService
{
    Task<IEnumerable<IdentityServerHost.Models.IdentityRole>> GetAllAsync();
    Task<IEnumerable<IdentityServerHost.Models.IdentityRole>> GetAllAsync(int page, int pageSize, string? search);
    Task<int> GetCountAsync(string? search);
    Task<IdentityServerHost.Models.IdentityRole?> GetByIdAsync(Guid id);
    Task<IdentityServerHost.Models.IdentityRole?> GetByNameAsync(string name);
    Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(IdentityServerHost.Models.IdentityRole role);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(IdentityServerHost.Models.IdentityRole role);
    Task<(bool Success, IEnumerable<string> Errors)> DeleteAsync(Guid id);
}
