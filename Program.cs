/*
 Copyright (c) 2024 Iamshen . All rights reserved.

 Copyright (c) 2024 HigginsSoft, Alexander Higgins - https://github.com/alexhiggins732/ 

 Copyright (c) 2018, Brock Allen & Dominick Baier. All rights reserved.

 Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information. 
 Source code and license this software can be found 

 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.
*/

using IdentityServerHost.Data;
using IdentityServerHost.HealthChecks;
using IdentityServerHost.Extentions;
using IdentityServerHost.Middleware;
using IdentityServerHost.Models;
using IdentityServerHost.SeedDatas;
using IdentityServerHost.Services;
using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Configuration;
using IdentityServerHost.Services.Identity;
using IdentityServerHost.Services.Ldap;
using IdentityServerHost.Services.Alerting;
using IdentityServerHost.Services.Operational;
using IdentityServerHost.Services.Sessions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
Console.Title = "DTI Identity Server";
Activity.DefaultIdFormat = ActivityIdFormat.W3C;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
    .CreateLogger();

try
{
    Log.Information("Starting host...");

    builder.Host.UseSerilog((context, services, config) =>
    {
        config
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
    });

    // Configuration
    var configuration = builder.Configuration;
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    var redisConnection = configuration.GetConnectionString("Redis");

    // Services (merged from Startup.ConfigureServices)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddDbContext<AuditDbContext>(options =>
        options.UseNpgsql(connectionString, sql => sql.MigrationsAssembly("IdentityServerHost")));

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddIdentity<ApplicationUser, IdentityServerHost.Models.IdentityRole>(options =>
    {
        options.Password.RequiredLength = configuration.GetValue("PasswordPolicy:MinLength", 8);
        options.Password.RequireDigit = configuration.GetValue("PasswordPolicy:RequireDigit", true);
        options.Password.RequireLowercase = configuration.GetValue("PasswordPolicy:RequireLowercase", true);
        options.Password.RequireUppercase = configuration.GetValue("PasswordPolicy:RequireUppercase", true);
        options.Password.RequireNonAlphanumeric = configuration.GetValue("PasswordPolicy:RequireNonAlphanumeric", true);
    })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddPasswordValidator<PasswordPolicyValidator>();

    builder.Services.Configure<IpWhitelistOptions>(configuration.GetSection("IpWhitelist"));
    builder.Services.Configure<LdapOptions>(configuration.GetSection("Ldap"));
    IdentityServerHost.Quickstart.UI.AccountOptions.RequireMfa = configuration.GetValue("AccountOptions:RequireMfa", false);

    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<ISessionService, SessionService>();
    builder.Services.AddScoped<IPersistedGrantService, PersistedGrantService>();
    builder.Services.AddScoped<IDeviceCodeService, DeviceCodeService>();
    builder.Services.AddSingleton<ILdapService, LdapService>();
    builder.Services.AddSingleton<IAlertService, AlertService>();
    builder.Services.AddTransient<IdentityServer4.Events.IEventSink, IdentityServerEventSink>();

    builder.Services.AddRateLimiting(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
        options.OnRejected = async (ctx, _) =>
        {
            ctx.HttpContext.Response.StatusCode = 429;
            await ctx.HttpContext.Response.WriteAsync("Too many requests.");
        };
    });

    var healthChecks = builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "database")
        .AddDbContextCheck<ApplicationDbContext>("appdb");

    if (!string.IsNullOrEmpty(redisConnection))
    {
        builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        healthChecks.AddCheck("redis", new RedisHealthCheck(redisConnection));
    }

    builder.Services.AddScoped<IClientConfigService, ClientConfigService>();
    builder.Services.AddScoped<IApiResourceConfigService, ApiResourceConfigService>();
    builder.Services.AddScoped<IApiScopeConfigService, ApiScopeConfigService>();
    builder.Services.AddScoped<IIdentityResourceConfigService, IdentityResourceConfigService>();
    builder.Services.AddScoped<IStatsService, StatsService>();

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("IdentityServerHost"))
        .WithMetrics(m =>
        {
            m.AddAspNetCoreInstrumentation();
            m.AddHttpClientInstrumentation();
            m.AddPrometheusExporter();
        });

    builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

    builder.Services.AddIdentityServer(options =>
    {
        options.Events.RaiseSuccessEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;

        options.EmitScopesAsSpaceDelimitedStringInJwt = true;

        options.MutualTls.Enabled = true;
        options.MutualTls.DomainName = "mtls";
        options.Csp.Level = CspLevel.Two;
    })
        .AddDeveloperSigningCredential()
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = builder => builder.UseNpgsql(connectionString,
                sql => sql.MigrationsAssembly("IdentityServerHost"));
        })
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = builder => builder.UseNpgsql(connectionString,
                sql => sql.MigrationsAssembly("IdentityServerHost"));
            options.EnableTokenCleanup = true;
            options.TokenCleanupInterval = 5;
        })
        .AddJwtBearerClientAuthentication()
        .AddMutualTlsSecretValidators()
        .AddConfigurationStoreCache()
        .AddAspNetIdentity<ApplicationUser>();

    var app = builder.Build();

    // Middleware (merged from Startup.Configure)
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseStaticFiles();

    app.UseRouting();

    app.UseIpWhitelist();
    app.UseRateLimiting();
    app.ConfigureCspAllowHeaders();

    app.MapHealthChecks("/health");
    app.MapPrometheusScrapingEndpoint();
    // Migrations & Seed
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            await auditDb.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Audit DB migration failed - ensure migrations exist. Run: dotnet ef migrations add InitialAuditDb -c AuditDbContext -o Migrations/AuditDb");
        }
    }
    await SeedClients.StartSeedAsync(app.Services);
    await SeedUsers.StartSeedAsync(app.Services);

    app.UseIdentityServer();

    app.UseAuthorization();

    app.MapDefaultControllerRoute();

    app.Run();
}
catch (Exception ex)
{
    // HostAbortedException thường xảy ra khi chạy dotnet ef (migrations) - EF tools build host rồi abort ngay, đây là hành vi bình thường
    if (ex is not Microsoft.Extensions.Hosting.HostAbortedException)
    {
        Log.Fatal(ex, "Host terminated unexpectedly.");
    }
}
finally
{
    Log.CloseAndFlush();
}