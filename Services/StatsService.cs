using IdentityServerHost.Data;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Services;

public class StatsService : IStatsService
{
    private readonly ApplicationDbContext _appDb;
    private readonly ConfigurationDbContext _configDb;
    private readonly PersistedGrantDbContext _grantDb;

    public StatsService(ApplicationDbContext appDb, ConfigurationDbContext configDb, PersistedGrantDbContext grantDb)
    {
        _appDb = appDb;
        _configDb = configDb;
        _grantDb = grantDb;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var usersCount = await _appDb.Users.CountAsync();
        var rolesCount = await _appDb.Roles.CountAsync();
        var clientsCount = await _configDb.Clients.CountAsync();
        var apiResourcesCount = await _configDb.ApiResources.CountAsync();
        var apiScopesCount = await _configDb.ApiScopes.CountAsync();
        var identityResourcesCount = await _configDb.IdentityResources.CountAsync();
        var persistedGrantsCount = await _grantDb.PersistedGrants.CountAsync();
        var activeClientsCount = await _configDb.Clients.CountAsync(c => c.Enabled);

        var clientsByGrantType = await _configDb.Clients
            .Include(c => c.AllowedGrantTypes)
            .SelectMany(c => c.AllowedGrantTypes)
            .GroupBy(g => g.GrantType)
            .Select(g => new GrantTypeStat { GrantType = g.Key ?? "unknown", Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var resourcesOverview = new List<ResourceTypeStat>
        {
            new() { Category = "API Resources", Count = apiResourcesCount },
            new() { Category = "API Scopes", Count = apiScopesCount },
            new() { Category = "Identity Resources", Count = identityResourcesCount }
        };

        return new DashboardStats
        {
            UsersCount = usersCount,
            RolesCount = rolesCount,
            ClientsCount = clientsCount,
            ApiResourcesCount = apiResourcesCount,
            ApiScopesCount = apiScopesCount,
            IdentityResourcesCount = identityResourcesCount,
            PersistedGrantsCount = persistedGrantsCount,
            ActiveClientsCount = activeClientsCount,
            ClientsByGrantType = clientsByGrantType,
            ResourcesOverview = resourcesOverview
        };
    }
}
