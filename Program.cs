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
using IdentityServerHost.Models;
using IdentityServerHost.SeedDatas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Xml.Linq;

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

    // Services (merged from Startup.ConfigureServices)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddIdentity<ApplicationUser, IdentityServerHost.Models.IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.AddControllersWithViews();

    builder.Services.AddIdentityServer(options =>
    {
        options.Events.RaiseSuccessEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;

        options.EmitScopesAsSpaceDelimitedStringInJwt = true;

        // options.MutualTls.Enabled = true;
        // options.MutualTls.DomainName = "mtls";
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
        // .AddMutualTlsSecretValidators()
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

    // Seed users and apply migrations at startup
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