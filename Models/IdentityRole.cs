using Microsoft.AspNetCore.Identity;

namespace IdentityServerHost.Models
{
    public class IdentityRole : IdentityRole<Guid>
    {
        public string? Code { get; set; }
    }
}
