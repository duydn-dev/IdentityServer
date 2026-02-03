using IdentityServer4.EntityFramework.Entities;
using IdentityServerHost.Models.ViewModels.Configuration;
using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

[Authorize]
[SecurityHeaders]
public class ApiScopesController : Controller
{
    private readonly IApiScopeConfigService _service;
    private readonly IAuditService _auditService;

    public ApiScopesController(IApiScopeConfigService service, IAuditService auditService)
    {
        _service = service;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null)
    {
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var items = await _service.GetAllAsync(page, pageSize, search);
        var total = await _service.GetCountAsync(search);
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ApiScopeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApiScopeViewModel model)
    {
        if (ModelState.IsValid)
        {
            var scope = MapToEntity(model);
            if (await _service.CreateAsync(scope))
            {
                await _auditService.LogAsync("ApiScope.Create", "ApiScope", scope.Name ?? "", $"Name={model.Name}", true);
                TempData["Success"] = "Thêm API Scope thành công.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, "Tên đã tồn tại.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var scope = await _service.GetByIdAsync(id);
        if (scope == null) return NotFound();
        return View(MapToViewModel(scope));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ApiScopeViewModel model)
    {
        if (ModelState.IsValid)
        {
            var scope = MapToEntity(model);
            if (await _service.UpdateAsync(scope))
            {
                await _auditService.LogAsync("ApiScope.Update", "ApiScope", scope.Name ?? "", $"Name={model.Name}", true);
                TempData["Success"] = "Cập nhật API Scope thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(int id)
    {
        var scopeBeforeDelete = await _service.GetByIdAsync(id);
        var success = await _service.DeleteAsync(id);
        if (success) await _auditService.LogAsync("ApiScope.Delete", "ApiScope", scopeBeforeDelete?.Name ?? id.ToString(), null, true);
        return Json(new { success, message = success ? "Xóa thành công." : "Không thể xóa." });
    }

    private static IdentityServer4.EntityFramework.Entities.ApiScope MapToEntity(ApiScopeViewModel model)
    {
        return new IdentityServer4.EntityFramework.Entities.ApiScope
        {
            Id = model.Id,
            Name = model.Name,
            DisplayName = model.DisplayName,
            Description = model.Description,
            Enabled = model.Enabled,
            Required = model.Required,
            Emphasize = model.Emphasize,
            ShowInDiscoveryDocument = model.ShowInDiscoveryDocument,
            UserClaims = ParseComma(model.UserClaimsText).Select(c => new ApiScopeClaim { Type = c }).ToList()
        };
    }

    private static ApiScopeViewModel MapToViewModel(IdentityServer4.EntityFramework.Entities.ApiScope scope)
    {
        return new ApiScopeViewModel
        {
            Id = scope.Id,
            Name = scope.Name ?? string.Empty,
            DisplayName = scope.DisplayName,
            Description = scope.Description,
            Enabled = scope.Enabled,
            Required = scope.Required,
            Emphasize = scope.Emphasize,
            ShowInDiscoveryDocument = scope.ShowInDiscoveryDocument,
            UserClaimsText = string.Join(", ", scope.UserClaims?.Select(c => c.Type) ?? Array.Empty<string>())
        };
    }

    private static List<string> ParseComma(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }
}
