using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

[Authorize]
[SecurityHeaders]
public class SessionsController : Controller
{
    private readonly ISessionService _sessionService;
    private readonly IAuditService _audit;

    public SessionsController(ISessionService sessionService, IAuditService audit)
    {
        _sessionService = sessionService;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var sessions = await _sessionService.GetUserSessionsAsync(userId);
        return View(sessions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(string sessionId)
    {
        var ok = await _sessionService.RevokeSessionAsync(sessionId);
        if (ok)
        {
            await _audit.LogAsync("RevokeSession", "Session", sessionId, "Session revoked");
            TempData["Success"] = "Đã đăng xuất phiên.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAll()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var count = await _sessionService.RevokeAllUserSessionsAsync(userId);
        await _audit.LogAsync("RevokeAllSessions", "Session", userId, $"Revoked {count} sessions");
        TempData["Success"] = $"Đã đăng xuất {count} phiên.";
        return RedirectToAction(nameof(Index));
    }
}
