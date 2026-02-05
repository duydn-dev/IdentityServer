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
public class RolesController : Controller
{
    private readonly IRoleService _roleService;
    private readonly IAuditService _auditService;

    public RolesController(IRoleService roleService, IAuditService auditService)
    {
        _roleService = roleService;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null)
    {
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var roles = await _roleService.GetAllAsync(page, pageSize, search);
        var total = await _roleService.GetCountAsync(search);
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(roles);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new RoleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleViewModel model)
    {
        if (ModelState.IsValid)
        {
            var role = new IdentityServerHost.Models.IdentityRole
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                NormalizedName = model.Code,
                Code = model.Code,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            (bool success, IEnumerable<string> errors) = await _roleService.CreateAsync(role);
            if (success)
            {
                await _auditService.LogAsync("Role.Create", "Role", role.Id.ToString(), $"Name={model.Name}", true);
                TempData["Success"] = "Thêm vai trò thành công.";
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
        var role = await _roleService.GetByIdAsync(id);
        if (role == null)
            return NotFound();

        var model = new RoleViewModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Code = role.Code
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleViewModel model)
    {
        if (model.Id == null)
            return NotFound();

        if (ModelState.IsValid)
        {
            var role = await _roleService.GetByIdAsync(model.Id.Value);
            if (role == null)
                return NotFound();

            role.Name = model.Name;
            role.NormalizedName = model.Code;
            role.Code = model.Code;

            var (success, errors) = await _roleService.UpdateAsync(role);
            if (success)
            {
                await _auditService.LogAsync("Role.Update", "Role", role.Id.ToString(), $"Name={model.Name}", true);
                TempData["Success"] = "Cập nhật vai trò thành công.";
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
        var (success, errors) = await _roleService.DeleteAsync(id);
        if (success)
        {
            await _auditService.LogAsync("Role.Delete", "Role", id.ToString(), null, true);
            TempData["Success"] = "Xóa vai trò thành công.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = string.Join("; ", errors);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(Guid id)
    {
        var (success, errors) = await _roleService.DeleteAsync(id);
        if (success) await _auditService.LogAsync("Role.Delete", "Role", id.ToString(), null, true);
        return Json(new { success, message = success ? "Xóa thành công." : string.Join("; ", errors) });
    }
}
