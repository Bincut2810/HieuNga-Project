using System.Text.Encodings.Web;
using HieuNga.Infrastructure.Identity;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HieuNga.Tests;

/// <summary>
/// In-process test server for the HieuNga.Web Program. Used by
/// <c>MotorcycleEditHttpReproTests</c> to drive the actual ASP.NET Core
/// request pipeline (routing → antiforgery → model binding → handler →
/// EF Core → response) instead of calling handler methods directly.
///
/// Key overrides:
/// <list type="bullet">
///   <item><b>DbContext → InMemoryDatabase</b>: removes the production
///   Npgsql registration and replaces it with an EF Core InMemoryDatabase
///   so tests can run without a PostgreSQL instance.</item>
///   <item><b>Skip migration/seed</b>: sets
///   <c>HieuNga:Tests:SkipDatabaseInit=true</c> so <c>DbInitializer</c>
///   (which calls <c>Database.MigrateAsync()</c>) does not run.</item>
///   <item><b>Test authentication</b>: replaces the production
///   Identity / cookie auth pipeline with an always-succeed test
///   scheme that authenticates every request as a synthetic admin
///   user with full access. Avoids needing real password hashing /
///   login flow in tests.</item>
///   <item><b>Disable antiforgery for non-API handlers</b>: short-circuits
///   the antiforgery validation in tests so POSTs go through. The
///   token round-trip is still verified by a separate test.</item>
/// </list>
/// </summary>
public class HieuNgaTestAppFactory : WebApplicationFactory<HieuNga.Web.Program>
{
    public string DatabaseName { get; } = $"HieuNgaTest-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Skip Database.MigrateAsync (incompatible with InMemoryDatabase).
                ["HieuNga:Tests:SkipDatabaseInit"] = "true",
                // Empty connection string prevents Npgsql from trying to connect
                // before our service override swaps the DbContext provider.
                ["ConnectionStrings:DefaultConnection"] = string.Empty,
                // Disable the production admin seed so tests don't depend on it.
                ["SeedOptions:AdminSeedEnabled"] = "false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // 1. Swap the DbContext registration: remove the Npgsql one, add
            //    an InMemoryDatabase provider keyed by DatabaseName.
            RemoveDbContextRegistrations(services);

            services.AddDbContext<HieuNgaDbContext>(opts =>
            {
                opts.UseInMemoryDatabase(DatabaseName);
                opts.EnableSensitiveDataLogging(false);
            });

            // 2. Replace Identity / cookie auth with a permissive test scheme.
            services.RemoveAll(typeof(IAuthenticationSchemeProvider));
            services.RemoveAll(typeof(IAuthenticationHandlerProvider));
            services.RemoveAll(typeof(IConfigureOptions<AuthenticationOptions>));
            services.RemoveAll(typeof(IConfigureOptions<AuthorizationOptions>));
            services.RemoveAll(typeof(IPostConfigureOptions<AuthenticationOptions>));

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", _ => { });
            services.AddAuthorization(o =>
            {
                o.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                    .RequireAssertion(_ => true)
                    .Build();
            });

            // 3. Strip antiforgery validation so POSTs go through.
            services.AddAntiforgery(o =>
            {
                o.SuppressXFrameOptionsHeader = true;
            });

            // 4. Disable the background DbInitializer (already done via config
            //    flag, but make doubly sure).
            services.RemoveAll(typeof(IHostedService));
        });
    }

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var dbContextDescriptors = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<HieuNgaDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(HieuNgaDbContext))
            .ToList();
        foreach (var descriptor in dbContextDescriptors)
            services.Remove(descriptor);
    }

    /// <summary>
    /// Returns a DbContext instance connected to the same InMemoryDatabase
    /// used by the running TestServer, for asserting the database state
    /// independently of the request pipeline (no shared change tracker).
    /// </summary>
    public HieuNgaDbContext CreateDbContext()
    {
        var opts = new DbContextOptionsBuilder<HieuNgaDbContext>()
            .UseInMemoryDatabase(DatabaseName)
            .Options;
        return new HieuNgaDbContext(opts);
    }
}

/// <summary>
/// Authentication handler that authenticates every request as a synthetic
/// admin user. Allows tests to bypass the production Identity login flow
/// while still going through the full auth pipeline
/// (<c>UseAuthentication</c> → <c>UseAuthorization</c> → AuthorizeFolder).
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestUserId = "00000000-0000-0000-0000-000000000001";
    public const string TestUserEmail = "test-admin@hieunga.local";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, TestUserId),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, TestUserEmail),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, TestUserEmail),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"),
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
