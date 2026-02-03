using IdentityServer4.Events;

namespace IdentityServerHost.Services.Alerting;

/// <summary>Chuyển tiếp sự kiện IdentityServer sang AlertService để giám sát. Đồng thời log như default sink.</summary>
public class IdentityServerEventSink : IEventSink
{
    private readonly IAlertService _alertService;
    private readonly ILogger<IdentityServerEventSink> _logger;

    public IdentityServerEventSink(IAlertService alertService, ILogger<IdentityServerEventSink> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    public async Task PersistAsync(Event evt)
    {
        var logLevel = evt.EventType switch
        {
            EventTypes.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            EventTypes.Failure => Microsoft.Extensions.Logging.LogLevel.Warning,
            EventTypes.Success => Microsoft.Extensions.Logging.LogLevel.Information,
            _ => Microsoft.Extensions.Logging.LogLevel.Debug
        };
        _logger.Log(logLevel, "IdentityServer event: {EventId} {Name} - {Message}", evt.Id, evt.Name, evt.Message);

        if (evt.EventType == EventTypes.Failure && evt.Name == "User Login Failure")
            await _alertService.RaiseAsync(AlertLevel.Warning, "Auth", "Đăng nhập thất bại", evt.Message);
        else if (evt.EventType == EventTypes.Error)
            await _alertService.RaiseAsync(AlertLevel.Error, "IdentityServer", evt.Name ?? "Error", evt.Message);
        await Task.CompletedTask;
    }
}
