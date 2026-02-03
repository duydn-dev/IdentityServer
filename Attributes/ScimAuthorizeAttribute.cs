using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace IdentityServerHost.Attributes;

/// <summary>Cho phép xác thực qua Bearer token (OAuth) hoặc API key (X-Scim-Api-Key / Authorization: Bearer &lt;key&gt;).</summary>
public class ScimAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            return;

        var apiKey = context.HttpContext.Request.Headers["X-Scim-Api-Key"].FirstOrDefault()
            ?? context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();

        var configApiKey = context.HttpContext.RequestServices.GetService<IConfiguration>()?["Scim:ApiKey"];
        if (!string.IsNullOrEmpty(configApiKey) && apiKey == configApiKey)
        {
            var identity = new ClaimsIdentity("ScimApiKey");
            identity.AddClaim(new Claim(ClaimTypes.Name, "scim-api"));
            context.HttpContext.User = new ClaimsPrincipal(identity);
            return;
        }

        context.Result = new UnauthorizedObjectResult(new { detail = "Invalid or missing SCIM API key or Bearer token." });
        await Task.CompletedTask;
    }
}
