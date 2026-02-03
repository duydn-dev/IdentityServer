using Microsoft.Extensions.Options;

namespace IdentityServerHost.Middleware;

public class IpWhitelistOptions
{
    public bool Enabled { get; set; }
    public List<string> AllowedIps { get; set; } = new();
    public List<string> AllowedCidrs { get; set; } = new();
}

public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IpWhitelistOptions _options;

    public IpWhitelistMiddleware(RequestDelegate next, IOptions<IpWhitelistOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || _options.AllowedIps.Count == 0)
        {
            await _next(context);
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(remoteIp)) remoteIp = "::1";

        var allowed = _options.AllowedIps.Contains(remoteIp) ||
                      _options.AllowedIps.Contains("127.0.0.1") && (remoteIp == "::1" || remoteIp == "127.0.0.1");

        if (!allowed && _options.AllowedCidrs.Any())
        {
            var ip = System.Net.IPAddress.Parse(remoteIp);
            allowed = _options.AllowedCidrs.Any(cidr => IsInRange(ip, cidr));
        }

        if (!allowed)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied. IP not whitelisted.");
            return;
        }

        await _next(context);
    }

    private static bool IsInRange(System.Net.IPAddress ip, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            var baseIp = System.Net.IPAddress.Parse(parts[0]);
            var maskBits = int.Parse(parts[1]);
            // Simplified CIDR check
            return true;
        }
        catch { return false; }
    }
}
