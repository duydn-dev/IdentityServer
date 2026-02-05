/*
 Copyright (c) 2024 Iamshen . All rights reserved.

 Copyright (c) 2024 HigginsSoft, Alexander Higgins - https://github.com/alexhiggins732/ 

 Copyright (c) 2018, Brock Allen & Dominick Baier. All rights reserved.

 Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information. 
 Source code and license this software can be found 

 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.
*/

using IdentityServerHost.Data;
using IdentityServerHost.Models;
using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Ldap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityServerHost.Quickstart.UI;

/// <summary>
/// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
/// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
/// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
/// </summary>
[SecurityHeaders]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _appContext;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clientStore;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IEventService _events;
    private readonly ILdapService _ldapService;
    private readonly LdapOptions _ldapOptions;
    private readonly IAuditService _auditService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext appContext,
        IIdentityServerInteractionService interaction,
        IClientStore clientStore,
        IAuthenticationSchemeProvider schemeProvider,
        IEventService events,
        ILdapService ldapService,
        IOptions<LdapOptions> ldapOptions,
        IAuditService auditService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _appContext = appContext;
        _interaction = interaction;
        _clientStore = clientStore;
        _schemeProvider = schemeProvider;
        _events = events;
        _ldapService = ldapService;
        _ldapOptions = ldapOptions.Value;
        _auditService = auditService;
    }

    /// <summary>
    /// Entry point into the login workflow
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Login(string returnUrl)
    {
        // build a model so we know what to show on the login page
        var vm = await BuildLoginViewModelAsync(returnUrl);

        if (vm.IsExternalLoginOnly)
        {
            // we only have one option for logging in and it's an external provider
            return returnUrl.IsAllowedRedirect() ? RedirectToAction("Challenge", "External", new { scheme = vm.ExternalLoginScheme, returnUrl = returnUrl.SanitizeForRedirect() }) : Forbid();
        }

        return View(vm);
    }

    /// <summary>
    /// Handle postback from username/password login
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel model)
    {
        // check if we are in the context of an authorization request
        var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

        if (ModelState.IsValid)
        {
            ApplicationUser? user = null;
            bool isLdapUser = false;

            user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // Local DB auth success
            }
            else if (_ldapOptions.Enabled)
            {
                var ldapResult = await _ldapService.AuthenticateAsync(model.Username, model.Password);
                if (ldapResult.Success)
                {
                    user = await _userManager.FindByNameAsync(model.Username);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            Id = Guid.NewGuid(),
                            UserName = model.Username,
                            Email = ldapResult.Email,
                            EmailConfirmed = !string.IsNullOrEmpty(ldapResult.Email),
                            SecurityStamp = Guid.NewGuid().ToString()
                        };
                        var createResult = await _userManager.CreateAsync(user, Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "!aA1");
                        if (!createResult.Succeeded)
                        {
                            ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản từ LDAP.");
                            var loginVm = await BuildLoginViewModelAsync(model);
                            return View(loginVm);
                        }
                    }
                    isLdapUser = true;
                }
                else
                    user = null;
            }
            else
                user = null;

            if (user != null)
            {
                if (AccountOptions.RequireMfa && !user.TwoFactorEnabled)
                {
                    ModelState.AddModelError(string.Empty, "MFA bắt buộc. Vui lòng liên hệ quản trị viên để kích hoạt xác thực hai yếu tố cho tài khoản của bạn.");
                    var loginVm = await BuildLoginViewModelAsync(model);
                    return View(loginVm);
                }

                if (user.TwoFactorEnabled && !isLdapUser)
                {
                    var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberLogin, lockoutOnFailure: false);
                    if (signInResult.RequiresTwoFactor)
                    {
                        return RedirectToAction(nameof(LoginWith2fa), new { returnUrl = model.ReturnUrl, rememberMe = model.RememberLogin });
                    }
                }

                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName!, user.Id.ToString(), user.UserName!, clientId: context?.Client.ClientId));
                await _auditService.LogAsync("Login.Success", "User", user.Id.ToString(), $"UserName={user.UserName}", true);

                AuthenticationProperties props = null;
                if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                {
                    props = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                    };
                }

                // Lấy roles của user để thêm vào claims
                var roles = await _appContext.UserRoles.Where(ur => ur.UserId == user.Id).AsNoTracking()
                    .Join(_appContext.Roles.AsNoTracking(),
                    l => l.RoleId,
                    r => r.Id,
                    (l, r) => r.NormalizedName).ToListAsync();

                var roleClaims = roles.Select(r => new Claim(JwtClaimTypes.Role, r ?? "")).ToList();

                var isuser = new IdentityServerUser(user.Id.ToString())
                {
                    DisplayName = user.UserName,
                    AdditionalClaims = roleClaims
                };

                await HttpContext.SignInAsync(isuser, props);

                if (context != null)
                {
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        return model.ReturnUrl.IsAllowedRedirect() ? this.LoadingPage("Redirect", model.ReturnUrl.SanitizeForRedirect()) : Forbid();
                    }

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return model.ReturnUrl.IsAllowedRedirect() ? Redirect(model.ReturnUrl.SanitizeForRedirect()) : Forbid();
                }

                // request for a local page
                if (Url.IsLocalUrl(model.ReturnUrl))
                {
                    return model.ReturnUrl.IsAllowedRedirect() ? Redirect(model.ReturnUrl.SanitizeForRedirect()) : Forbid();
                }
                else if (string.IsNullOrEmpty(model.ReturnUrl))
                {
                    return model.ReturnUrl.IsAllowedRedirect() ? Redirect("~/") : Forbid();
                }
                else
                {
                    // user might have clicked on a malicious link - should be logged
                    throw new Exception("invalid return URL");
                }
            }

            await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
            await _auditService.LogAsync("Login.Failure", "User", null, $"Username={model.Username}", false);
            ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
        }

        // something went wrong, show form with error
        var vm = await BuildLoginViewModelAsync(model);
        return View(vm);
    }

    /// <summary>
    /// Handle postback from username/password login
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginCancel(LoginInputModel model)
    {
        // check if we are in the context of an authorization request
        var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

        if (context != null)
        {
            // if the user cancels, send a result back into IdentityServer as if they 
            // denied the consent (even if this client does not require consent).
            // this will send back an access denied OIDC error response to the client.
            await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

            // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
            if (context.IsNativeClient())
            {
                // The client is native, so this change in how to
                // return the response is for better UX for the end user.
                return model.ReturnUrl.IsAllowedRedirect() ? this.LoadingPage("Redirect", model.ReturnUrl.SanitizeForRedirect()) : Forbid();
            }

            return model.ReturnUrl.IsAllowedRedirect() ? Redirect(model.ReturnUrl.SanitizeForRedirect()) : Forbid();
        }
        else
        {
            // since we don't have a valid context, then we just go back to the home page
            return model.ReturnUrl.IsAllowedRedirect() ? Redirect("~/") : Forbid();
        }

    }

    /// <summary>
    /// Show logout page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Logout(string logoutId)
    {
        // build a model so the logout page knows what to display
        var vm = await BuildLogoutViewModelAsync(logoutId);

        if (vm.ShowLogoutPrompt == false)
        {
            // if the request for logout was properly authenticated from IdentityServer, then
            // we don't need to show the prompt and can just log the user out directly.
            return await Logout(vm);
        }

        return View(vm);
    }

    /// <summary>
    /// Handle logout page postback
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(LogoutInputModel model)
    {
        // build a model so the logged out page knows what to display
        var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

        if (User?.Identity.IsAuthenticated == true)
        {
            var subjectId = User.GetSubjectId();
            var displayName = User.GetDisplayName();
            await HttpContext.SignOutAsync();
            await _signInManager.SignOutAsync();
            await _events.RaiseAsync(new UserLogoutSuccessEvent(subjectId, displayName));
            await _auditService.LogAsync("Logout", "User", subjectId, $"UserName={displayName}", true);
        }

        // check if we need to trigger sign-out at an upstream identity provider
        if (vm.TriggerExternalSignout)
        {
            // build a return URL so the upstream provider will redirect back
            // to us after the user has logged out. this allows us to then
            // complete our single sign-out processing.
            string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

            // this triggers a redirect to the external provider for sign-out
            return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
        }

        return View("LoggedOut", vm);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> LoginWith2fa(string returnUrl, bool rememberMe = false)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToAction(nameof(Login), new { returnUrl });

        return View(new IdentityServer4.Models.AccountViewModels.LoginWith2faViewModel
        {
            ReturnUrl = returnUrl,
            RememberMe = rememberMe
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(IdentityServer4.Models.AccountViewModels.LoginWith2faViewModel model)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.TwoFactorCode, model.RememberMe, model.RememberMachine);
        if (result.Succeeded)
        {
            await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName!, user.Id.ToString(), user.UserName!));
            await _auditService.LogAsync("Login.Success", "User", user.Id.ToString(), $"UserName={user.UserName} (2FA)", true);

            AuthenticationProperties props = null;
            if (model.RememberMe)
            {
                props = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                };
            }
            // Lấy roles của user để thêm vào claims
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(r => new Claim(JwtClaimTypes.Role, r)).ToList();

            var isuser = new IdentityServerUser(user.Id.ToString()) 
            { 
                DisplayName = user.UserName,
                AdditionalClaims = roleClaims
            };
            await HttpContext.SignInAsync(isuser, props);

            return model.ReturnUrl.IsAllowedRedirect() ? Redirect(model.ReturnUrl.SanitizeForRedirect()) : Redirect("~/");
        }
        else if (result.IsLockedOut)
        {
            return RedirectToAction(nameof(Lockout));
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Mã xác thực không hợp lệ.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToAction(nameof(Login), new { returnUrl });

        return View(new IdentityServer4.Models.AccountViewModels.LoginWithRecoveryCodeViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(IdentityServer4.Models.AccountViewModels.LoginWithRecoveryCodeViewModel model)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(model.RecoveryCode.Replace(" ", ""));
        if (result.Succeeded)
        {
            await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName!, user.Id.ToString(), user.UserName!));
            await _auditService.LogAsync("Login.Success", "User", user.Id.ToString(), $"UserName={user.UserName} (Recovery)", true);

            // Lấy roles của user để thêm vào claims
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(r => new Claim(JwtClaimTypes.Role, r)).ToList();

            var isuser = new IdentityServerUser(user.Id.ToString()) 
            { 
                DisplayName = user.UserName,
                AdditionalClaims = roleClaims
            };
            await HttpContext.SignInAsync(isuser);

            return model.ReturnUrl.IsAllowedRedirect() ? Redirect(model.ReturnUrl.SanitizeForRedirect()) : Redirect("~/");
        }
        ModelState.AddModelError(string.Empty, "Mã khôi phục không hợp lệ.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Lockout()
    {
        return View();
    }

    /*****************************************/
    /* helper APIs for the AccountController */
    /*****************************************/
    private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
    {
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
        {
            var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

            // this is meant to short circuit the UI and only trigger the one external IdP
            var vm = new LoginViewModel
            {
                EnableLocalLogin = local,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
            };

            if (!local)
            {
                vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
            }

            return vm;
        }

        var schemes = await _schemeProvider.GetAllSchemesAsync();

        var providers = schemes
            .Where(x => x.DisplayName != null)
            .Select(x => new ExternalProvider
            {
                DisplayName = x.DisplayName ?? x.Name,
                AuthenticationScheme = x.Name
            }).ToList();

        var allowLocal = true;
        if (context?.Client.ClientId != null)
        {
            var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
            if (client != null)
            {
                allowLocal = client.EnableLocalLogin;

                if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                {
                    providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                }
            }
        }

        return new LoginViewModel
        {
            AllowRememberLogin = AccountOptions.AllowRememberLogin,
            EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
            ReturnUrl = returnUrl,
            Username = context?.LoginHint,
            ExternalProviders = providers.ToArray()
        };
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
    {
        var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
        vm.Username = model.Username;
        vm.RememberLogin = model.RememberLogin;
        return vm;
    }

    private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
    {
        var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

        if (User?.Identity.IsAuthenticated != true)
        {
            // if the user is not authenticated, then just show logged out page
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        var context = await _interaction.GetLogoutContextAsync(logoutId);
        if (context?.ShowSignoutPrompt == false)
        {
            // it's safe to automatically sign-out
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        // show the logout prompt. this prevents attacks where the user
        // is automatically signed out by another malicious web page.
        return vm;
    }

    private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
    {
        // get context information (client name, post logout redirect URI and iframe for federated signout)
        var logout = await _interaction.GetLogoutContextAsync(logoutId);

        var vm = new LoggedOutViewModel
        {
            AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
            PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
            ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
            SignOutIframeUrl = logout?.SignOutIFrameUrl,
            LogoutId = logoutId
        };

        if (User?.Identity.IsAuthenticated == true)
        {
            var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
            if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
            {
                var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                if (providerSupportsSignout)
                {
                    if (vm.LogoutId == null)
                    {
                        // if there's no current logout context, we need to create one
                        // this captures necessary info from the current logged in user
                        // before we signout and redirect away to the external IdP for signout
                        vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                    }

                    vm.ExternalAuthenticationScheme = idp;
                }
            }
        }

        return vm;
    }
}
