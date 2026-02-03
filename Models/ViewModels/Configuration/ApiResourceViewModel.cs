using System.ComponentModel.DataAnnotations;

namespace IdentityServerHost.Models.ViewModels.Configuration;

public class ApiResourceViewModel
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

    [Display(Name = "Hiển thị trong Discovery")]
    public bool ShowInDiscoveryDocument { get; set; } = true;

    [Display(Name = "Scopes (phân cách bằng dấu phẩy)")]
    public string ScopesText { get; set; } = string.Empty;

    [Display(Name = "User Claims (phân cách bằng dấu phẩy)")]
    public string UserClaimsText { get; set; } = string.Empty;
}
