using System.Security.Cryptography;
using System.Text;

namespace IdentityServerHost.Services.Configuration;

public static class SecretHasher
{
    public static string Sha256(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }
}
