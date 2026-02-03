namespace IdentityServerHost.Services.Sessions;

public interface ISessionService
{
    Task<IEnumerable<SessionInfo>> GetUserSessionsAsync(string userId);
    Task<bool> RevokeSessionAsync(string sessionId);
    Task<int> RevokeAllUserSessionsAsync(string userId);
}

public class SessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? SubjectId { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? Expiration { get; set; }
    public string? Type { get; set; }
}
