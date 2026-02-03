using IdentityServerHost.Models;

namespace IdentityServerHost.Services.Identity;

public interface IUserService
{
    Task<IEnumerable<ApplicationUser>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<int> GetCountAsync(string? search = null);
    Task<ApplicationUser?> GetByIdAsync(Guid id);
    Task<ApplicationUser?> GetByUserNameAsync(string userName);
    Task<IList<string>> GetUserRolesAsync(Guid userId);
    Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(ApplicationUser user, string password, IEnumerable<string>? roles = null);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(ApplicationUser user, string? newPassword, IEnumerable<string>? roles = null);
    Task<(bool Success, IEnumerable<string> Errors)> DeleteAsync(Guid id);
}
