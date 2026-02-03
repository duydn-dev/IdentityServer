using Microsoft.Extensions.Options;

namespace IdentityServerHost.Services.Ldap;

public class LdapOptions
{
    public bool Enabled { get; set; }
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 389;
    public string BaseDn { get; set; } = "dc=example,dc=com";
    public string? BindDn { get; set; }
    public string? BindPassword { get; set; }
    public string UserFilter { get; set; } = "(&(objectClass=user)(sAMAccountName={0}))";
}

public class LdapService : ILdapService
{
    private readonly LdapOptions _options;

    public LdapService(IOptions<LdapOptions> options)
    {
        _options = options.Value;
    }

    public async Task<LdapAuthResult> AuthenticateAsync(string username, string password)
    {
        if (!_options.Enabled)
            return new LdapAuthResult { Success = false, Message = "LDAP is disabled" };

        return await Task.Run(() =>
        {
            try
            {
                using var conn = new Novell.Directory.Ldap.LdapConnection();
                conn.Connect(_options.Server, _options.Port);
                conn.ConnectionTimeout = 5000;

                if (!string.IsNullOrEmpty(_options.BindDn) && !string.IsNullOrEmpty(_options.BindPassword))
                {
                    conn.Bind(_options.BindDn, _options.BindPassword);
                }

                var searchFilter = string.Format(_options.UserFilter, username);
                var searchResults = conn.Search(_options.BaseDn, Novell.Directory.Ldap.LdapConnection.ScopeSub, searchFilter, null, false);

                if (!searchResults.HasMore())
                    return new LdapAuthResult { Success = false, Message = "User not found" };

                var userEntry = searchResults.Next();
                conn.Bind(userEntry.Dn, password);

                var displayName = userEntry.GetAttribute("displayName")?.StringValue;
                var email = userEntry.GetAttribute("mail")?.StringValue ?? userEntry.GetAttribute("userPrincipalName")?.StringValue;

                return new LdapAuthResult
                {
                    Success = true,
                    DisplayName = displayName,
                    Email = email
                };
            }
            catch (Novell.Directory.Ldap.LdapException ex)
            {
                return new LdapAuthResult { Success = false, Message = ex.Message };
            }
        });
    }
}
