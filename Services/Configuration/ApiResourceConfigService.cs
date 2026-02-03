using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using ApiResourceEntity = IdentityServer4.EntityFramework.Entities.ApiResource;

namespace IdentityServerHost.Services.Configuration;

public class ApiResourceConfigService : IApiResourceConfigService
{
    private readonly ConfigurationDbContext _context;

    public ApiResourceConfigService(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ApiResourceEntity>> GetAllAsync(int page, int pageSize, string? search)
    {
        var query = _context.ApiResources.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(r =>
                (r.Name != null && r.Name.ToLower().Contains(s)) ||
                (r.DisplayName != null && r.DisplayName.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Scopes)
            .Include(r => r.UserClaims)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? search)
    {
        var query = _context.ApiResources.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(r =>
                (r.Name != null && r.Name.ToLower().Contains(s)) ||
                (r.DisplayName != null && r.DisplayName.ToLower().Contains(s)));
        }
        return await query.CountAsync();
    }

    public async Task<ApiResourceEntity?> GetByIdAsync(int id)
    {
        return await _context.ApiResources
            .Include(r => r.Scopes)
            .Include(r => r.UserClaims)
            .Include(r => r.Secrets)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ApiResourceEntity?> GetByNameAsync(string name)
    {
        return await _context.ApiResources.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<bool> CreateAsync(ApiResourceEntity resource)
    {
        if (await GetByNameAsync(resource.Name) != null)
            return false;

        resource.Created = DateTime.UtcNow;
        _context.ApiResources.Add(resource);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(ApiResourceEntity resource)
    {
        var existing = await _context.ApiResources
            .Include(r => r.Scopes)
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == resource.Id);

        if (existing == null) return false;

        existing.Enabled = resource.Enabled;
        existing.DisplayName = resource.DisplayName;
        existing.Description = resource.Description;
        existing.ShowInDiscoveryDocument = resource.ShowInDiscoveryDocument;
        existing.Updated = DateTime.UtcNow;

        existing.Scopes.Clear();
        foreach (var scope in resource.Scopes)
        {
            existing.Scopes.Add(new ApiResourceScope { Scope = scope.Scope });
        }

        existing.UserClaims.Clear();
        foreach (var claim in resource.UserClaims)
        {
            existing.UserClaims.Add(new ApiResourceClaim { Type = claim.Type });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var resource = await _context.ApiResources.FindAsync(id);
        if (resource == null) return false;

        _context.ApiResources.Remove(resource);
        await _context.SaveChangesAsync();
        return true;
    }
}
