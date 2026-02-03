using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using ApiScopeEntity = IdentityServer4.EntityFramework.Entities.ApiScope;

namespace IdentityServerHost.Services.Configuration;

public class ApiScopeConfigService : IApiScopeConfigService
{
    private readonly ConfigurationDbContext _context;

    public ApiScopeConfigService(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ApiScopeEntity>> GetAllAsync(int page, int pageSize, string? search)
    {
        var query = _context.ApiScopes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(s)) ||
                (x.DisplayName != null && x.DisplayName.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.UserClaims)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? search)
    {
        var query = _context.ApiScopes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(s)) ||
                (x.DisplayName != null && x.DisplayName.ToLower().Contains(s)));
        }
        return await query.CountAsync();
    }

    public async Task<ApiScopeEntity?> GetByIdAsync(int id)
    {
        return await _context.ApiScopes
            .Include(x => x.UserClaims)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ApiScopeEntity?> GetByNameAsync(string name)
    {
        return await _context.ApiScopes.FirstOrDefaultAsync(x => x.Name == name);
    }

    public async Task<bool> CreateAsync(ApiScopeEntity scope)
    {
        if (await GetByNameAsync(scope.Name) != null)
            return false;

        _context.ApiScopes.Add(scope);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(ApiScopeEntity scope)
    {
        var existing = await _context.ApiScopes
            .Include(x => x.UserClaims)
            .FirstOrDefaultAsync(x => x.Id == scope.Id);

        if (existing == null) return false;

        existing.Enabled = scope.Enabled;
        existing.DisplayName = scope.DisplayName;
        existing.Description = scope.Description;
        existing.Required = scope.Required;
        existing.Emphasize = scope.Emphasize;
        existing.ShowInDiscoveryDocument = scope.ShowInDiscoveryDocument;

        existing.UserClaims.Clear();
        foreach (var claim in scope.UserClaims)
        {
            existing.UserClaims.Add(new ApiScopeClaim { Type = claim.Type });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var scope = await _context.ApiScopes.FindAsync(id);
        if (scope == null) return false;

        _context.ApiScopes.Remove(scope);
        await _context.SaveChangesAsync();
        return true;
    }
}
