using IdentityServerHost.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IdentityModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServerHost.SeedDatas
{
    public class SeedUsers
    {
        public static async Task StartSeedAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                if (context is null) throw new Exception("ApplicationDbContext is null");
                try
                {
                    await context.Database.MigrateAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityServerHost.Models.ApplicationUser>>();
                var alice = await userMgr.FindByNameAsync("admin");
                if (alice == null)
                {
                    alice = new Models.ApplicationUser
                    {
                        UserName = "admin"
                    };
                    var result = await userMgr.CreateAsync(alice, "Duy12345@");
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }

                    result = await userMgr.AddClaimsAsync(alice, new Claim[]{
                            new Claim(JwtClaimTypes.Name, "Quản trị viên"),
                            new Claim(JwtClaimTypes.GivenName, "Quản"),
                            new Claim(JwtClaimTypes.FamilyName, "trị viên"),
                            new Claim(JwtClaimTypes.Email, "admin@gmail.com"),
                            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean)
                    });
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }
                    Console.WriteLine("admin created");
                }
                else
                {
                    Console.WriteLine("admin already exists");
                }
            }
        }
    }
}
