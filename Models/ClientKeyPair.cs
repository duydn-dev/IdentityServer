using System.ComponentModel.DataAnnotations;

namespace IdentityServerHost.Models;

/// <summary>
/// Lưu trữ RSA key pair cho mỗi client để xác thực API requests
/// </summary>
public class ClientKeyPair
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ClientId từ IdentityServer4 (liên kết với bảng Clients)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// RSA Private Key (PKCS#8 format) - Base64 encoded
    /// </summary>
    [Required]
    public string PrivateKey { get; set; } = null!;

    /// <summary>
    /// RSA Public Key - Base64 encoded
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = null!;

    /// <summary>
    /// Key size in bits (2048, 4096, etc.)
    /// </summary>
    public int KeySize { get; set; } = 2048;

    /// <summary>
    /// Thời gian tạo key
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian hết hạn (null = không hết hạn)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Key có đang active không
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Mô tả/ghi chú
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}
