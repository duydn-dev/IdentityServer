using IdentityServerHost.Models;
using IdentityServerHost.Services.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

[Authorize]
[SecurityHeaders]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditService _auditService;
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    public ManageController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IAuditService auditService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> EnableAuthenticator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var sharedKey = FormatKey(unformattedKey!);
        var authenticatorUri = string.Format(AuthenticatorUriFormat, "DTI Identity", user.UserName ?? user.Email ?? user.Id.ToString(), unformattedKey);

        var model = new IdentityServer4.Models.ManageViewModels.EnableAuthenticatorViewModel
        {
            SharedKey = sharedKey,
            AuthenticatorUri = authenticatorUri
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(IdentityServer4.Models.ManageViewModels.EnableAuthenticatorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.SharedKey = FormatKey(await _userManager.GetAuthenticatorKeyAsync(user) ?? "");
            return View(model);
        }

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);
        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, "Mã xác thực không hợp lệ.");
            model.SharedKey = FormatKey(await _userManager.GetAuthenticatorKeyAsync(user) ?? "");
            return View(model);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _auditService.LogAsync("MFA.Enable", "User", user.Id.ToString(), $"UserName={user.UserName}", true);
        TempData["Success"] = "Đã kích hoạt xác thực hai yếu tố.";

        return RedirectToAction(nameof(GenerateRecoveryCodes));
    }

    [HttpGet]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!user.TwoFactorEnabled)
            return RedirectToAction(nameof(EnableAuthenticator));

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        var model = new IdentityServer4.Models.ManageViewModels.GenerateRecoveryCodesViewModel
        {
            RecoveryCodes = codes!.ToArray()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Disable2fa()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!user.TwoFactorEnabled)
        {
            TempData["Error"] = "2FA chưa được bật.";
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable2faConfirm()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _auditService.LogAsync("MFA.Disable", "User", user.Id.ToString(), $"UserName={user.UserName}", true);
        TempData["Success"] = "Đã tắt xác thực hai yếu tố.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var model = new IdentityServer4.Models.ManageViewModels.IndexViewModel
        {
            HasPassword = true,
            TwoFactorEnabled = user.TwoFactorEnabled,
            AuthenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user)
        };
        return View(model);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
            result.Append(unformattedKey.AsSpan(currentPosition));
        return result.ToString().ToLowerInvariant();
    }
}
