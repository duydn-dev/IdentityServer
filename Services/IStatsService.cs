namespace IdentityServerHost.Services;

public interface IStatsService
{
    Task<DashboardStats> GetDashboardStatsAsync();
}

public class DashboardStats
{
    public int UsersCount { get; set; }
    public int RolesCount { get; set; }
    public int ClientsCount { get; set; }
    public int ApiResourcesCount { get; set; }
    public int ApiScopesCount { get; set; }
    public int IdentityResourcesCount { get; set; }
    public int PersistedGrantsCount { get; set; }
    public int ActiveClientsCount { get; set; }
    public List<GrantTypeStat> ClientsByGrantType { get; set; } = new();
    public List<ResourceTypeStat> ResourcesOverview { get; set; } = new();
}

public class GrantTypeStat
{
    public string GrantType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ResourceTypeStat
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}
