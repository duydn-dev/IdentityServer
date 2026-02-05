using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityServerHost.Services.ClientKeys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityServerHost.Attributes;

/// <summary>
/// Xác thực SCIM API requests bằng cách kiểm tra API key (public key) với cặp key trong Redis/DB.
/// Client gửi public key qua header X-Scim-Api-Key hoặc Authorization: Bearer {public_key}
/// Server sẽ tìm cặp key tương ứng và verify rằng private/public key là một cặp hợp lệ.
/// </summary>
public class ScimAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Nếu đã authenticated qua cách khác (cookie, JWT...) thì cho qua
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            return;

        var logger = context.HttpContext.RequestServices.GetService<ILogger<ScimAuthorizeAttribute>>();

        // Lấy API key từ header
        var apiKey = context.HttpContext.Request.Headers["X-Scim-Api-Key"].FirstOrDefault()
            ?? context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { detail = "Missing API key. Provide X-Scim-Api-Key header or Authorization: Bearer {public_key}" });
            return;
        }

        // Fallback: Kiểm tra static API key từ config (backward compatibility)
        var configApiKey = context.HttpContext.RequestServices.GetService<IConfiguration>()?["Scim:ApiKey"];
        if (!string.IsNullOrEmpty(configApiKey) && apiKey == configApiKey)
        {
            var identity = new ClaimsIdentity("ScimApiKey");
            identity.AddClaim(new Claim(ClaimTypes.Name, "scim-api"));
            identity.AddClaim(new Claim("auth_method", "static_key"));
            context.HttpContext.User = new ClaimsPrincipal(identity);
            return;
        }

        // Tìm key pair trong Redis/DB bằng public key
        var keyService = context.HttpContext.RequestServices.GetService<IClientKeyService>();
        if (keyService == null)
        {
            logger?.LogError("IClientKeyService not registered");
            context.Result = new UnauthorizedObjectResult(new { detail = "Service configuration error." });
            return;
        }

        // Tìm client có public key khớp (sử dụng cache Redis)
        var keyPair = await keyService.GetByPublicKeyAsync(apiKey);
        if (keyPair == null)
        {
            logger?.LogWarning("No client found with provided public key");
            context.Result = new UnauthorizedObjectResult(new { detail = "Invalid API key. No matching client found." });
            return;
        }

        // Kiểm tra key còn active không
        if (!keyPair.IsActive)
        {
            logger?.LogWarning("Client {ClientId} key is deactivated", keyPair.ClientId);
            context.Result = new UnauthorizedObjectResult(new { detail = "API key has been deactivated." });
            return;
        }

        // Kiểm tra hết hạn
        if (keyPair.ExpiresAt.HasValue && keyPair.ExpiresAt.Value < DateTime.UtcNow)
        {
            logger?.LogWarning("Client {ClientId} key has expired", keyPair.ClientId);
            context.Result = new UnauthorizedObjectResult(new { detail = "API key has expired." });
            return;
        }

        // Verify private key và public key là một cặp hợp lệ
        if (!VerifyKeyPair(keyPair.PrivateKey, keyPair.PublicKey))
        {
            logger?.LogError("Client {ClientId} has invalid key pair (private/public mismatch)", keyPair.ClientId);
            context.Result = new UnauthorizedObjectResult(new { detail = "Invalid key pair configuration." });
            return;
        }

        // Xác thực thành công - set identity
        var clientIdentity = new ClaimsIdentity("ScimClientKey");
        clientIdentity.AddClaim(new Claim(ClaimTypes.Name, $"client:{keyPair.ClientId}"));
        clientIdentity.AddClaim(new Claim("client_id", keyPair.ClientId));
        clientIdentity.AddClaim(new Claim("auth_method", "rsa_key_pair"));
        clientIdentity.AddClaim(new Claim(ClaimTypes.Role, "scim_client"));
        context.HttpContext.User = new ClaimsPrincipal(clientIdentity);

        logger?.LogInformation("SCIM request authenticated for client {ClientId}", keyPair.ClientId);
    }

    /// <summary>
    /// Verify rằng private key và public key là một cặp hợp lệ
    /// Bằng cách sign một message test với private key rồi verify với public key
    /// </summary>
    private static bool VerifyKeyPair(string privateKeyBase64, string publicKeyBase64)
    {
        try
        {
            // Test message
            var testMessage = Encoding.UTF8.GetBytes("key_pair_verification_test");

            // Sign với private key
            using var rsaPrivate = RSA.Create();
            rsaPrivate.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
            var signature = rsaPrivate.SignData(testMessage, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Verify với public key
            using var rsaPublic = RSA.Create();
            rsaPublic.ImportRSAPublicKey(Convert.FromBase64String(publicKeyBase64), out _);
            return rsaPublic.VerifyData(testMessage, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
}
