using System.Security.Cryptography;
using System.Text;
using IdentityServerHost.Data;
using IdentityServerHost.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace IdentityServerHost.Services.ClientKeys;

public class ClientKeyService : IClientKeyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ClientKeyService> _logger;
    
    private const string CacheKeyPrefix = "client_key:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    public ClientKeyService(
        ApplicationDbContext dbContext,
        IDistributedCache cache,
        ILogger<ClientKeyService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ClientKeyPair> GenerateKeyPairAsync(string clientId, int keySize = 2048, string? description = null, DateTime? expiresAt = null)
    {
        // Kiểm tra xem đã có key cho client này chưa
        var existingKey = await _dbContext.ClientKeyPairs.FirstOrDefaultAsync(k => k.ClientId == clientId);
        if (existingKey != null)
        {
            // Vô hiệu hóa key cũ
            existingKey.IsActive = false;
            await _dbContext.SaveChangesAsync();
            await InvalidateCacheAsync(clientId);
        }

        // Tạo RSA key pair mới
        using var rsa = RSA.Create(keySize);
        byte[] privateKeyPkcs8 = rsa.ExportPkcs8PrivateKey();
        byte[] publicKey = rsa.ExportRSAPublicKey();

        var keyPair = new ClientKeyPair
        {
            ClientId = clientId,
            PrivateKey = Convert.ToBase64String(privateKeyPkcs8),
            PublicKey = Convert.ToBase64String(publicKey),
            KeySize = keySize,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true,
            Description = description
        };

        _dbContext.ClientKeyPairs.Add(keyPair);
        await _dbContext.SaveChangesAsync();

        // Cache key pair
        await SetCacheAsync(clientId, keyPair);

        _logger.LogInformation("Generated new RSA key pair for client {ClientId}", clientId);

        return keyPair;
    }

    public async Task<ClientKeyPair?> GetByClientIdAsync(string clientId)
    {
        // Thử lấy từ cache trước
        var cached = await GetFromCacheAsync(clientId);
        if (cached != null)
        {
            _logger.LogDebug("Client key for {ClientId} found in cache", clientId);
            return cached;
        }

        // Không có trong cache, lấy từ DB
        var keyPair = await _dbContext.ClientKeyPairs
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.ClientId == clientId && k.IsActive);

        if (keyPair != null)
        {
            // Kiểm tra hết hạn
            if (keyPair.ExpiresAt.HasValue && keyPair.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Client key for {ClientId} has expired", clientId);
                return null;
            }

            // Cache lại
            await SetCacheAsync(clientId, keyPair);
            _logger.LogDebug("Client key for {ClientId} loaded from database and cached", clientId);
        }

        return keyPair;
    }

    public async Task<bool> VerifySignatureAsync(string clientId, byte[] data, byte[] signature)
    {
        var keyPair = await GetByClientIdAsync(clientId);
        if (keyPair == null)
        {
            _logger.LogWarning("No active key found for client {ClientId}", clientId);
            return false;
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(keyPair.PublicKey), out _);
            
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature for client {ClientId}", clientId);
            return false;
        }
    }

    public async Task<string?> GetPublicKeyAsync(string clientId)
    {
        var keyPair = await GetByClientIdAsync(clientId);
        return keyPair?.PublicKey;
    }

    public async Task<ClientKeyPair?> GetByPublicKeyAsync(string publicKey)
    {
        // Thử tìm trong cache trước (cache theo public key)
        var cacheKey = $"{CacheKeyPrefix}pubkey:{publicKey.GetHashCode()}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cached))
        {
            try
            {
                var cachedKeyPair = System.Text.Json.JsonSerializer.Deserialize<ClientKeyPair>(cached);
                if (cachedKeyPair != null && cachedKeyPair.PublicKey == publicKey)
                {
                    _logger.LogDebug("Key pair found in cache by public key");
                    return cachedKeyPair;
                }
            }
            catch { }
        }

        // Tìm trong DB
        var keyPair = await _dbContext.ClientKeyPairs
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.PublicKey == publicKey && k.IsActive);

        if (keyPair != null)
        {
            // Kiểm tra hết hạn
            if (keyPair.ExpiresAt.HasValue && keyPair.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Key pair for client {ClientId} found but expired", keyPair.ClientId);
                return null;
            }

            // Cache lại
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration
            };
            var json = System.Text.Json.JsonSerializer.Serialize(keyPair);
            await _cache.SetStringAsync(cacheKey, json, options);
            
            _logger.LogDebug("Key pair for client {ClientId} found by public key and cached", keyPair.ClientId);
        }

        return keyPair;
    }

    public async Task<bool> DeactivateKeyAsync(string clientId)
    {
        var keyPair = await _dbContext.ClientKeyPairs.FirstOrDefaultAsync(k => k.ClientId == clientId);
        if (keyPair == null) return false;

        keyPair.IsActive = false;
        await _dbContext.SaveChangesAsync();
        await InvalidateCacheAsync(clientId);

        _logger.LogInformation("Deactivated key for client {ClientId}", clientId);
        return true;
    }

    public async Task InvalidateCacheAsync(string clientId)
    {
        var cacheKey = $"{CacheKeyPrefix}{clientId}";
        await _cache.RemoveAsync(cacheKey);
        _logger.LogDebug("Cache invalidated for client {ClientId}", clientId);
    }

    public async Task<IEnumerable<ClientKeyPair>> GetAllAsync()
    {
        return await _dbContext.ClientKeyPairs
            .AsNoTracking()
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(string clientId)
    {
        var keyPair = await _dbContext.ClientKeyPairs.FirstOrDefaultAsync(k => k.ClientId == clientId);
        if (keyPair == null) return false;

        _dbContext.ClientKeyPairs.Remove(keyPair);
        await _dbContext.SaveChangesAsync();
        await InvalidateCacheAsync(clientId);

        _logger.LogInformation("Deleted key for client {ClientId}", clientId);
        return true;
    }

    #region Private Methods

    private async Task<ClientKeyPair?> GetFromCacheAsync(string clientId)
    {
        var cacheKey = $"{CacheKeyPrefix}{clientId}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (string.IsNullOrEmpty(cached))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ClientKeyPair>(cached);
        }
        catch
        {
            return null;
        }
    }

    private async Task SetCacheAsync(string clientId, ClientKeyPair keyPair)
    {
        var cacheKey = $"{CacheKeyPrefix}{clientId}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration
        };

        var json = System.Text.Json.JsonSerializer.Serialize(keyPair);
        await _cache.SetStringAsync(cacheKey, json, options);
    }

    #endregion
}
