using System.ComponentModel.DataAnnotations;

namespace IdentityServerHost.Models.ViewModels.Configuration;

public class IdentityResourceViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên là bắt buộc")]
    [Display(Name = "Tên")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Tên hiển thị")]
    [StringLength(200)]
    public string? DisplayName { get; set; }

    [Display(Name = "Mô tả")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Bật")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Bắt buộc")]
    public bool Required { get; set; }

    [Display(Name = "Nhấn mạnh")]
    public bool Emphasize { get; set; }

    [Display(Name = "Hiển thị trong Discovery")]
    public bool ShowInDiscoveryDocument { get; set; } = true;

    [Display(Name = "User Claims (phân cách bằng dấu phẩy)")]
    public string UserClaimsText { get; set; } = string.Empty;
}
