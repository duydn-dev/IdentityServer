using IdentityServerHost.Attributes;
using IdentityServerHost.Constants;
using IdentityServerHost.Models;
using IdentityServerHost.Models.ViewModels;
using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

[Authorize(Roles = Roles.Admin)]
[SecurityHeaders]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IAuditService _auditService;

    public UsersController(IUserService userService, IRoleService roleService, IAuditService auditService)
    {
        _userService = userService;
        _roleService = roleService;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null)
    {
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var users = await _userService.GetAllAsync(page, pageSize, search);
        var total = await _userService.GetCountAsync(search);
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var roles = await _roleService.GetAllAsync();
        ViewBag.Roles = roles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToList();
        return View(new UserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserViewModel model)
    {
        var roles = await _roleService.GetAllAsync();
        ViewBag.Roles = roles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToList();

        if (string.IsNullOrEmpty(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Mật khẩu là bắt buộc khi tạo mới.");
        else if (model.Password != model.ConfirmPassword)
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp.");

        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = model.EmailConfirmed,
                PhoneNumberConfirmed = model.PhoneNumberConfirmed,
                TwoFactorEnabled = model.TwoFactorEnabled,
                LockoutEnabled = model.LockoutEnabled
            };

            var (success, errors) = await _userService.CreateAsync(user, model.Password!, model.SelectedRoles);
            if (success)
            {
                await _auditService.LogAsync("User.Create", "User", user.Id.ToString(), $"UserName={model.UserName}", true);
                TempData["Success"] = "Thêm người dùng thành công.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in errors)
                ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        var userRoles = await _userService.GetUserRolesAsync(id);
        var roles = await _roleService.GetAllAsync();

        ViewBag.Roles = roles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToList();

        var model = new UserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            SelectedRoles = userRoles.ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserViewModel model)
    {
        var roles = await _roleService.GetAllAsync();
        ViewBag.Roles = roles.Select(r => r.Name).Where(n => n != null).Cast<string>().ToList();

        if (model.Id == null)
            return NotFound();

        var existing = await _userService.GetByIdAsync(model.Id.Value);
        if (existing == null)
            return NotFound();

        if (!string.IsNullOrEmpty(model.Password) && model.Password != model.ConfirmPassword)
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp.");

        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                Id = existing.Id,
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = model.EmailConfirmed,
                PhoneNumberConfirmed = model.PhoneNumberConfirmed,
                TwoFactorEnabled = model.TwoFactorEnabled,
                LockoutEnabled = model.LockoutEnabled,
                LockoutEnd = existing.LockoutEnd,
                AccessFailedCount = existing.AccessFailedCount
            };

            var (success, errors) = await _userService.UpdateAsync(user, model.Password, model.SelectedRoles);
            if (success)
            {
                await _auditService.LogAsync("User.Update", "User", user.Id.ToString(), $"UserName={model.UserName}", true);
                TempData["Success"] = "Cập nhật người dùng thành công.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in errors)
                ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, errors) = await _userService.DeleteAsync(id);
        if (success)
        {
            await _auditService.LogAsync("User.Delete", "User", id.ToString(), null, true);
            TempData["Success"] = "Xóa người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = string.Join("; ", errors);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(Guid id)
    {
        var (success, errors) = await _userService.DeleteAsync(id);
        if (success) await _auditService.LogAsync("User.Delete", "User", id.ToString(), null, true);
        return Json(new { success, message = success ? "Xóa thành công." : string.Join("; ", errors) });
    }
}
