using System.ComponentModel.DataAnnotations;

namespace IdentityServerHost.Models.ViewModels;

public class RoleViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Tên vai trò là bắt buộc")]
    [Display(Name = "Tên vai trò")]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mã")]
    [StringLength(50)]
    public string? Code { get; set; }
}
