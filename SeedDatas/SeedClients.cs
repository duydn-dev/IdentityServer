using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServerHost.Configuration;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.SeedDatas
{
    public class SeedClients
    {
        public static async Task StartSeedAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = scope.ServiceProvider.GetService<ConfigurationDbContext>())
                {
                    await EnsureSeedData(context);
                }
            }
        }

        private static async Task EnsureSeedData(ConfigurationDbContext? context)
        {
            if (context is null) throw new Exception("ConfigurationDbContext is null");

            Console.WriteLine("Seeding database...");

            if (!(await context.Clients.AnyAsync()))
            {
                Console.WriteLine("Clients being populated");
                foreach (var client in Clients.Get())
                {
                    await context.Clients.AddAsync(client.ToEntity());
                }
                await context.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine("Clients already populated");
            }

            if (!(await context.IdentityResources.AnyAsync()))
            {
                Console.WriteLine("IdentityResources being populated");
                foreach (var resource in Configuration.Resources.IdentityResources)
                {
                    await context.IdentityResources.AddAsync(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine("IdentityResources already populated");
            }

            if (!(await context.ApiResources.AnyAsync()))
            {
                Console.WriteLine("ApiResources being populated");
                foreach (var resource in Configuration.Resources.ApiResources)
                {
                    await context.ApiResources.AddAsync(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine("ApiResources already populated");
            }

            if (!(await context.ApiScopes.AnyAsync()))  
            {
                Console.WriteLine("Scopes being populated");
                foreach (var resource in Configuration.Resources.ApiScopes)
                {
                    await context.ApiScopes.AddAsync(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine("Scopes already populated");
            }

            Console.WriteLine("Done seeding database.");
            Console.WriteLine();
        }
    }

}
