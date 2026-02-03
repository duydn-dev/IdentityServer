using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using IdentityResourceEntity = IdentityServer4.EntityFramework.Entities.IdentityResource;

namespace IdentityServerHost.Services.Configuration;

public class IdentityResourceConfigService : IIdentityResourceConfigService
{
    private readonly ConfigurationDbContext _context;

    public IdentityResourceConfigService(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<IdentityResourceEntity>> GetAllAsync(int page, int pageSize, string? search)
    {
        var query = _context.IdentityResources.AsNoTracking();

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
            .Include(r => r.UserClaims)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? search)
    {
        var query = _context.IdentityResources.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(r =>
                (r.Name != null && r.Name.ToLower().Contains(s)) ||
                (r.DisplayName != null && r.DisplayName.ToLower().Contains(s)));
        }
        return await query.CountAsync();
    }

    public async Task<IdentityResourceEntity?> GetByIdAsync(int id)
    {
        return await _context.IdentityResources
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IdentityResourceEntity?> GetByNameAsync(string name)
    {
        return await _context.IdentityResources.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<bool> CreateAsync(IdentityResourceEntity resource)
    {
        if (await GetByNameAsync(resource.Name) != null)
            return false;

        resource.Created = DateTime.UtcNow;
        _context.IdentityResources.Add(resource);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(IdentityResourceEntity resource)
    {
        var existing = await _context.IdentityResources
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == resource.Id);

        if (existing == null) return false;

        existing.Enabled = resource.Enabled;
        existing.DisplayName = resource.DisplayName;
        existing.Description = resource.Description;
        existing.Required = resource.Required;
        existing.Emphasize = resource.Emphasize;
        existing.ShowInDiscoveryDocument = resource.ShowInDiscoveryDocument;
        existing.Updated = DateTime.UtcNow;

        existing.UserClaims.Clear();
        foreach (var claim in resource.UserClaims)
        {
            existing.UserClaims.Add(new IdentityResourceClaim { Type = claim.Type });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var resource = await _context.IdentityResources.FindAsync(id);
        if (resource == null) return false;

        _context.IdentityResources.Remove(resource);
        await _context.SaveChangesAsync();
        return true;
    }
}
