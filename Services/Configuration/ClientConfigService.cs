using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using ClientEntity = IdentityServer4.EntityFramework.Entities.Client;

namespace IdentityServerHost.Services.Configuration;

public class ClientConfigService : IClientConfigService
{
    private readonly ConfigurationDbContext _context;

    public ClientConfigService(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClientEntity>> GetAllAsync(int page, int pageSize, string? search)
    {
        var query = _context.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                (c.ClientId != null && c.ClientId.ToLower().Contains(s)) ||
                (c.ClientName != null && c.ClientName.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(c => c.ClientId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.AllowedScopes)
            .Include(c => c.RedirectUris)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? search)
    {
        var query = _context.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                (c.ClientId != null && c.ClientId.ToLower().Contains(s)) ||
                (c.ClientName != null && c.ClientName.ToLower().Contains(s)));
        }
        return await query.CountAsync();
    }

    public async Task<ClientEntity?> GetByIdAsync(int id)
    {
        return await _context.Clients
            .Include(c => c.AllowedScopes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.ClientSecrets)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ClientEntity?> GetByClientIdAsync(string clientId)
    {
        return await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public async Task<bool> CreateAsync(ClientEntity client)
    {
        if (await GetByClientIdAsync(client.ClientId) != null)
            return false;

        client.Created = DateTime.UtcNow;
        if (string.IsNullOrEmpty(client.ProtocolType))
            client.ProtocolType = "oidc";
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(ClientEntity client)
    {
        var existing = await _context.Clients
            .Include(c => c.AllowedScopes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.ClientSecrets)
            .FirstOrDefaultAsync(c => c.Id == client.Id);

        if (existing == null) return false;

        existing.Enabled = client.Enabled;
        existing.ClientName = client.ClientName;
        existing.Description = client.Description;
        existing.ClientUri = client.ClientUri;
        existing.LogoUri = client.LogoUri;
        existing.RequireConsent = client.RequireConsent;
        existing.AllowRememberConsent = client.AllowRememberConsent;
        existing.RequirePkce = client.RequirePkce;
        existing.AllowOfflineAccess = client.AllowOfflineAccess;
        existing.Updated = DateTime.UtcNow;

        existing.AllowedScopes.Clear();
        foreach (var scope in client.AllowedScopes)
        {
            existing.AllowedScopes.Add(new ClientScope { Scope = scope.Scope });
        }

        existing.RedirectUris.Clear();
        foreach (var uri in client.RedirectUris)
        {
            existing.RedirectUris.Add(new ClientRedirectUri { RedirectUri = uri.RedirectUri });
        }

        existing.PostLogoutRedirectUris.Clear();
        foreach (var uri in client.PostLogoutRedirectUris)
        {
            existing.PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = uri.PostLogoutRedirectUri });
        }

        existing.AllowedCorsOrigins.Clear();
        foreach (var origin in client.AllowedCorsOrigins)
        {
            existing.AllowedCorsOrigins.Add(new ClientCorsOrigin { Origin = origin.Origin });
        }

        existing.AllowedGrantTypes.Clear();
        foreach (var gt in client.AllowedGrantTypes)
        {
            existing.AllowedGrantTypes.Add(new ClientGrantType { GrantType = gt.GrantType });
        }

        foreach (var secret in client.ClientSecrets!.Where(s => s.Id == 0))
        {
            existing.ClientSecrets.Add(secret);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null) return false;

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        return true;
    }
}
