using IdentityServerHost.Attributes;
using IdentityServerHost.Constants;
using IdentityServerHost.Services.Alerting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

[Authorize(Roles = Roles.Admin)]
[SecurityHeaders]
public class AlertsController : Controller
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    public async Task<IActionResult> Index(int count = 50)
    {
        var alerts = await _alertService.GetRecentAsync(count);
        return View(alerts);
    }

    [HttpGet]
    [Route("api/alerts")]
    public async Task<IActionResult> GetAlerts([FromQuery] int count = 20)
    {
        var alerts = await _alertService.GetRecentAsync(count);
        return Ok(alerts);
    }
}
