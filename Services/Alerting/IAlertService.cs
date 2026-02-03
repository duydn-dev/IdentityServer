namespace IdentityServerHost.Services.Alerting;

public interface IAlertService
{
    Task RaiseAsync(AlertLevel level, string category, string message, string? details = null);
    Task<IReadOnlyList<AlertRecord>> GetRecentAsync(int count = 50);
}

public enum AlertLevel { Info, Warning, Error, Critical }

public class AlertRecord
{
    public DateTime Timestamp { get; set; }
    public AlertLevel Level { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}
