using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityServerHost.Services.ClientKeys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityServerHost.Attributes;

/// <summary>
/// Attribute xác thực request từ external client sử dụng RSA signature.
/// Client cần gửi:
/// - Header "X-Client-Id": ClientId của client
/// - Header "X-Signature": Base64 signature của payload (timestamp + nonce + body)
/// - Header "X-Timestamp": Unix timestamp (milliseconds)
/// - Header "X-Nonce": Random string để chống replay attack
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ClientKeyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const int MaxTimestampDiffSeconds = 300; // 5 phút tolerance

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<ClientKeyAuthorizeAttribute>>();

        // Lấy headers
        var clientId = httpContext.Request.Headers["X-Client-Id"].FirstOrDefault();
        var signature = httpContext.Request.Headers["X-Signature"].FirstOrDefault();
        var timestampStr = httpContext.Request.Headers["X-Timestamp"].FirstOrDefault();
        var nonce = httpContext.Request.Headers["X-Nonce"].FirstOrDefault();

        // Validate required headers
        if (string.IsNullOrEmpty(clientId))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Missing X-Client-Id header" });
            return;
        }

        if (string.IsNullOrEmpty(signature))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Missing X-Signature header" });
            return;
        }

        if (string.IsNullOrEmpty(timestampStr) || !long.TryParse(timestampStr, out var timestamp))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid X-Timestamp header" });
            return;
        }

        if (string.IsNullOrEmpty(nonce))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Missing X-Nonce header" });
            return;
        }

        // Kiểm tra timestamp (chống replay attack)
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var diff = Math.Abs(now - timestamp);
        if (diff > MaxTimestampDiffSeconds * 1000)
        {
            logger.LogWarning("Request from client {ClientId} has expired timestamp. Diff: {Diff}ms", clientId, diff);
            context.Result = new UnauthorizedObjectResult(new { error = "Request timestamp expired" });
            return;
        }

        // Lấy service
        var keyService = httpContext.RequestServices.GetRequiredService<IClientKeyService>();

        // Lấy key từ cache/db
        var keyPair = await keyService.GetByClientIdAsync(clientId);
        if (keyPair == null)
        {
            logger.LogWarning("No active key found for client {ClientId}", clientId);
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid client or key not found" });
            return;
        }

        // Đọc body nếu có
        string body = "";
        if (httpContext.Request.ContentLength > 0)
        {
            httpContext.Request.EnableBuffering();
            using var reader = new StreamReader(
                httpContext.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);
            body = await reader.ReadToEndAsync();
            httpContext.Request.Body.Position = 0;
        }

        // Tạo payload để verify: timestamp + nonce + body
        var payload = $"{timestamp}{nonce}{body}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(signature);
        }
        catch
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid signature format" });
            return;
        }

        // Verify signature
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(keyPair.PublicKey), out _);

            var isValid = rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            if (!isValid)
            {
                logger.LogWarning("Invalid signature for client {ClientId}", clientId);
                context.Result = new UnauthorizedObjectResult(new { error = "Invalid signature" });
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying signature for client {ClientId}", clientId);
            context.Result = new UnauthorizedObjectResult(new { error = "Signature verification failed" });
            return;
        }

        // Thành công - set client identity
        var identity = new ClaimsIdentity("ClientKey");
        identity.AddClaim(new Claim("client_id", clientId));
        identity.AddClaim(new Claim(ClaimTypes.Name, $"client:{clientId}"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "external_client"));
        httpContext.User = new ClaimsPrincipal(identity);

        logger.LogInformation("Client {ClientId} authenticated successfully via RSA signature", clientId);
    }
}
