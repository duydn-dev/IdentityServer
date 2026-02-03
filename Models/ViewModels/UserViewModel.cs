using System.ComponentModel.DataAnnotations;

namespace IdentityServerHost.Models.ViewModels;

public class UserViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [Display(Name = "Tên đăng nhập")]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Xác nhận email")]
    public bool EmailConfirmed { get; set; }

    [Display(Name = "Xác nhận SĐT")]
    public bool PhoneNumberConfirmed { get; set; }

    [Display(Name = "Bật 2FA")]
    public bool TwoFactorEnabled { get; set; }

    [Display(Name = "Khóa đăng nhập")]
    public bool LockoutEnabled { get; set; }

    [Display(Name = "Mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6-100 ký tự")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name = "Xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Vai trò")]
    public List<string> SelectedRoles { get; set; } = new();
}
