using IdentityServerHost.Attributes;
using IdentityServerHost.Constants;
using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

[Authorize(Roles = Roles.Admin)]
[SecurityHeaders]
public class DeviceCodesController : Controller
{
    private readonly IDeviceCodeService _service;
    private readonly IAuditService _audit;

    public DeviceCodesController(IDeviceCodeService service, IAuditService audit)
    {
        _service = service;
        _audit = audit;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? userCode = null, string? clientId = null)
    {
        ViewBag.UserCode = userCode;
        ViewBag.ClientId = clientId;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var (items, total) = await _service.GetPagedAsync(page, pageSize, userCode, clientId);
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string userCode)
    {
        var ok = await _service.RemoveAsync(userCode);
        if (ok)
        {
            await _audit.LogAsync("RemoveDeviceCode", "DeviceFlowCodes", userCode, "Device code removed");
            TempData["Success"] = "Đã xóa device code.";
        }
        else
            TempData["Error"] = "Không tìm thấy.";
        return RedirectToAction(nameof(Index));
    }
}
