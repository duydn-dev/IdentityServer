namespace IdentityServerHost.Services.Ldap;

public interface ILdapService
{
    Task<LdapAuthResult> AuthenticateAsync(string username, string password);
}

public class LdapAuthResult
{
    public bool Success { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
}
