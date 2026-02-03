using System.Collections.Concurrent;

namespace IdentityServerHost.Services.Alerting;

public class AlertService : IAlertService
{
    private static readonly ConcurrentQueue<AlertRecord> _recent = new();
    private const int MaxRecent = 200;
    private readonly ILogger<AlertService> _logger;

    public AlertService(ILogger<AlertService> logger)
    {
        _logger = logger;
    }

    public Task RaiseAsync(AlertLevel level, string category, string message, string? details = null)
    {
        var record = new AlertRecord
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Category = category,
            Message = message,
            Details = details
        };
        _recent.Enqueue(record);
        while (_recent.Count > MaxRecent && _recent.TryDequeue(out _)) { }

        var logLevel = level switch
        {
            AlertLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
            AlertLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            AlertLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        };
        _logger.Log(logLevel, "[{Category}] {Message} {Details}", category, message, details ?? "");
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AlertRecord>> GetRecentAsync(int count = 50)
    {
        var list = _recent.TakeLast(Math.Min(count, MaxRecent)).ToList();
        return Task.FromResult<IReadOnlyList<AlertRecord>>(list);
    }
}
