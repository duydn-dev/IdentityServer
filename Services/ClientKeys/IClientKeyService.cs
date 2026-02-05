using IdentityServerHost.Models;

namespace IdentityServerHost.Services.ClientKeys;

public interface IClientKeyService
{
    /// <summary>
    /// Tạo RSA key pair mới cho client
    /// </summary>
    Task<ClientKeyPair> GenerateKeyPairAsync(string clientId, int keySize = 2048, string? description = null, DateTime? expiresAt = null);

    /// <summary>
    /// Lấy key pair theo ClientId (ưu tiên từ Redis cache)
    /// </summary>
    Task<ClientKeyPair?> GetByClientIdAsync(string clientId);

    /// <summary>
    /// Xác thực signature với public key của client
    /// </summary>
    Task<bool> VerifySignatureAsync(string clientId, byte[] data, byte[] signature);

    /// <summary>
    /// Lấy public key của client (dạng Base64)
    /// </summary>
    Task<string?> GetPublicKeyAsync(string clientId);

    /// <summary>
    /// Tìm key pair theo public key
    /// </summary>
    Task<ClientKeyPair?> GetByPublicKeyAsync(string publicKey);

    /// <summary>
    /// Vô hiệu hóa key pair
    /// </summary>
    Task<bool> DeactivateKeyAsync(string clientId);

    /// <summary>
    /// Xóa key pair khỏi cache
    /// </summary>
    Task InvalidateCacheAsync(string clientId);

    /// <summary>
    /// Lấy tất cả key pairs
    /// </summary>
    Task<IEnumerable<ClientKeyPair>> GetAllAsync();

    /// <summary>
    /// Xóa key pair
    /// </summary>
    Task<bool> DeleteAsync(string clientId);
}
