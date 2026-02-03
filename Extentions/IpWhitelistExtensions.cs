using IdentityServerHost.Middleware;
using Microsoft.AspNetCore.Builder;

namespace IdentityServerHost.Extentions;

public static class IpWhitelistExtensions
{
    public static IApplicationBuilder UseIpWhitelist(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IpWhitelistMiddleware>();
    }
}
