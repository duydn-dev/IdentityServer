using IdentityServer4.EntityFramework.Entities;
using IdentityServerHost.Attributes;
using IdentityServerHost.Constants;
using IdentityServerHost.Models.ViewModels.Configuration;
using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

[Authorize(Roles = Roles.Admin)]
[SecurityHeaders]
public class ApiResourcesController : Controller
{
    private readonly IApiResourceConfigService _service;
    private readonly IAuditService _auditService;

    public ApiResourcesController(IApiResourceConfigService service, IAuditService auditService)
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
        return View(new ApiResourceViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApiResourceViewModel model)
    {
        if (ModelState.IsValid)
        {
            var resource = MapToEntity(model);
            if (await _service.CreateAsync(resource))
            {
                await _auditService.LogAsync("ApiResource.Create", "ApiResource", resource.Name ?? "", $"Name={model.Name}", true);
                TempData["Success"] = "Thêm API Resource thành công.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, "Tên đã tồn tại.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var resource = await _service.GetByIdAsync(id);
        if (resource == null) return NotFound();
        return View(MapToViewModel(resource));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ApiResourceViewModel model)
    {
        if (ModelState.IsValid)
        {
            var resource = MapToEntity(model);
            if (await _service.UpdateAsync(resource))
            {
                await _auditService.LogAsync("ApiResource.Update", "ApiResource", resource.Name ?? "", $"Name={model.Name}", true);
                TempData["Success"] = "Cập nhật API Resource thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(int id)
    {
        var resourceBeforeDelete = await _service.GetByIdAsync(id);
        var success = await _service.DeleteAsync(id);
        if (success) await _auditService.LogAsync("ApiResource.Delete", "ApiResource", resourceBeforeDelete?.Name ?? id.ToString(), null, true);
        return Json(new { success, message = success ? "Xóa thành công." : "Không thể xóa." });
    }

    private static IdentityServer4.EntityFramework.Entities.ApiResource MapToEntity(ApiResourceViewModel model)
    {
        return new IdentityServer4.EntityFramework.Entities.ApiResource
        {
            Id = model.Id,
            Name = model.Name,
            DisplayName = model.DisplayName,
            Description = model.Description,
            Enabled = model.Enabled,
            ShowInDiscoveryDocument = model.ShowInDiscoveryDocument,
            Scopes = ParseComma(model.ScopesText).Select(s => new ApiResourceScope { Scope = s }).ToList(),
            UserClaims = ParseComma(model.UserClaimsText).Select(c => new ApiResourceClaim { Type = c }).ToList()
        };
    }

    private static ApiResourceViewModel MapToViewModel(IdentityServer4.EntityFramework.Entities.ApiResource resource)
    {
        return new ApiResourceViewModel
        {
            Id = resource.Id,
            Name = resource.Name ?? string.Empty,
            DisplayName = resource.DisplayName,
            Description = resource.Description,
            Enabled = resource.Enabled,
            ShowInDiscoveryDocument = resource.ShowInDiscoveryDocument,
            ScopesText = string.Join(", ", resource.Scopes?.Select(s => s.Scope) ?? Array.Empty<string>()),
            UserClaimsText = string.Join(", ", resource.UserClaims?.Select(c => c.Type) ?? Array.Empty<string>())
        };
    }

    private static List<string> ParseComma(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }
}
