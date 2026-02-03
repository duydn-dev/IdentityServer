using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace IdentityServerHost.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RedisHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = ConnectionMultiplexer.Connect(_connectionString);
            var db = conn.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis OK");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis", ex);
        }
    }
}
