using Microsoft.AspNetCore.Identity;

namespace IdentityServerHost.Services.Identity;

public class PasswordPolicyValidator : IPasswordValidator<IdentityServerHost.Models.ApplicationUser>
{
    public int MinLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;

    public Task<IdentityResult> ValidateAsync(UserManager<IdentityServerHost.Models.ApplicationUser> manager,
        IdentityServerHost.Models.ApplicationUser user, string password)
    {
        var errors = new List<IdentityError>();

        if (password.Length < MinLength)
            errors.Add(new IdentityError { Code = "PasswordTooShort", Description = $"Mật khẩu phải có ít nhất {MinLength} ký tự." });

        if (RequireDigit && !password.Any(char.IsDigit))
            errors.Add(new IdentityError { Code = "PasswordRequiresDigit", Description = "Mật khẩu phải chứa ít nhất một chữ số." });

        if (RequireLowercase && !password.Any(char.IsLower))
            errors.Add(new IdentityError { Code = "PasswordRequiresLower", Description = "Mật khẩu phải chứa ít nhất một chữ thường." });

        if (RequireUppercase && !password.Any(char.IsUpper))
            errors.Add(new IdentityError { Code = "PasswordRequiresUpper", Description = "Mật khẩu phải chứa ít nhất một chữ hoa." });

        if (RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            errors.Add(new IdentityError { Code = "PasswordRequiresNonAlphanumeric", Description = "Mật khẩu phải chứa ít nhất một ký tự đặc biệt." });

        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
    }
}
