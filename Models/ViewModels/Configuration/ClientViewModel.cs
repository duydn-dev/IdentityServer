using System.ComponentModel.DataAnnotations;

namespace IdentityServerHost.Models.ViewModels.Configuration;

public class ClientViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Client ID là bắt buộc")]
    [Display(Name = "Client ID")]
    [StringLength(200)]
    public string ClientId { get; set; } = string.Empty;

    [Display(Name = "Tên hiển thị")]
    [StringLength(200)]
    public string? ClientName { get; set; }

    [Display(Name = "Mô tả")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Client URI")]
    [StringLength(2000)]
    [Url]
    public string? ClientUri { get; set; }

    [Display(Name = "Logo URI")]
    [StringLength(2000)]
    [Url]
    public string? LogoUri { get; set; }

    [Display(Name = "Bật")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Yêu cầu consent")]
    public bool RequireConsent { get; set; }

    [Display(Name = "Cho phép ghi nhớ consent")]
    public bool AllowRememberConsent { get; set; } = true;

    [Display(Name = "Yêu cầu PKCE")]
    public bool RequirePkce { get; set; } = true;

    [Display(Name = "Cho phép Offline Access")]
    public bool AllowOfflineAccess { get; set; }

    [Display(Name = "Yêu cầu Client Secret")]
    public bool RequireClientSecret { get; set; } = true;

    [Display(Name = "Client Secret (để trống nếu không đổi)")]
    [DataType(DataType.Password)]
    [StringLength(4000)]
    public string? ClientSecret { get; set; }

    [Display(Name = "Grant Types")]
    public List<string> AllowedGrantTypes { get; set; } = new() { "authorization_code" };

    [Display(Name = "Allowed Scopes")]
    public List<string> AllowedScopes { get; set; } = new();

    [Display(Name = "Redirect URIs (mỗi dòng một URI)")]
    public string RedirectUrisText { get; set; } = string.Empty;

    [Display(Name = "Post Logout Redirect URIs")]
    public string PostLogoutRedirectUrisText { get; set; } = string.Empty;

    [Display(Name = "CORS Origins")]
    public string AllowedCorsOriginsText { get; set; } = string.Empty;
}
