using IdentityServerHost.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Controllers;

[Authorize]
[SecurityHeaders]
public class AuditLogsController : Controller
{
    private readonly AuditDbContext _db;

    public AuditLogsController(AuditDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 50, string? userId = null, string? action = null, DateTime? from = null, DateTime? to = null)
    {
        ViewBag.UserId = userId;
        ViewBag.Action = action;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId != null && a.UserId.Contains(userId));
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.Contains(action));
        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value.AddDays(1));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(items);
    }
}
