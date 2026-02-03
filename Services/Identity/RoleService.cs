using IdentityServerHost.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Services.Identity;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityServerHost.Models.IdentityRole> _roleManager;

    public RoleService(ApplicationDbContext dbContext, RoleManager<IdentityServerHost.Models.IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<IdentityServerHost.Models.IdentityRole>> GetAllAsync()
    {
        return await _dbContext.Roles.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<IEnumerable<IdentityServerHost.Models.IdentityRole>> GetAllAsync(int page, int pageSize, string? search)
    {
        var query = _dbContext.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(r =>
                (r.Name != null && r.Name.ToLower().Contains(searchLower)) ||
                (r.Code != null && r.Code.ToLower().Contains(searchLower)));
        }

        return await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? search)
    {
        var query = _dbContext.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(r =>
                (r.Name != null && r.Name.ToLower().Contains(searchLower)) ||
                (r.Code != null && r.Code.ToLower().Contains(searchLower)));
        }

        return await query.CountAsync();
    }

    public async Task<IdentityServerHost.Models.IdentityRole?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Roles.FindAsync(id);
    }

    public async Task<IdentityServerHost.Models.IdentityRole?> GetByNameAsync(string name)
    {
        return await _dbContext.Roles.FirstOrDefaultAsync(r => r.NormalizedName == name.ToUpperInvariant());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(IdentityServerHost.Models.IdentityRole role)
    {
        var result = await _roleManager.CreateAsync(role);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(IdentityServerHost.Models.IdentityRole role)
    {
        var existing = await _roleManager.FindByIdAsync(role.Id.ToString());
        if (existing == null)
            return (false, new[] { "Vai trò không tồn tại." });

        existing.Name = role.Name;
        existing.NormalizedName = role.Name?.ToUpperInvariant();
        existing.Code = role.Code;
        existing.ConcurrencyStamp = role.ConcurrencyStamp ?? Guid.NewGuid().ToString();

        var result = await _roleManager.UpdateAsync(existing);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> DeleteAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return (false, new[] { "Vai trò không tồn tại." });

        var result = await _roleManager.DeleteAsync(role);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }
}
