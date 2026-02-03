using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

[Authorize]
[SecurityHeaders]
public class PersistedGrantsController : Controller
{
    private readonly IPersistedGrantService _service;
    private readonly IAuditService _audit;

    public PersistedGrantsController(IPersistedGrantService service, IAuditService audit)
    {
        _service = service;
        _audit = audit;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? subjectId = null, string? clientId = null, string? type = null)
    {
        ViewBag.SubjectId = subjectId;
        ViewBag.ClientId = clientId;
        ViewBag.Type = type;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var (items, total) = await _service.GetPagedAsync(page, pageSize, subjectId, clientId, type);
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(string key)
    {
        var ok = await _service.RevokeAsync(key);
        if (ok)
        {
            await _audit.LogAsync("RevokeToken", "PersistedGrant", key, "Token revoked");
            TempData["Success"] = "Đã thu hồi token.";
        }
        else
            TempData["Error"] = "Không tìm thấy token.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RevokeAjax(string key)
    {
        var ok = await _service.RevokeAsync(key);
        if (ok) await _audit.LogAsync("RevokeToken", "PersistedGrant", key, "Token revoked");
        return Json(new { success = ok, message = ok ? "Đã thu hồi." : "Không tìm thấy." });
    }
}
