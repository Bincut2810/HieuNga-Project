using HieuNga.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Persistence;

/// <summary>
/// Production startup: apply EF migrations, then optionally ensure one admin account.
/// Never inserts motorcycles, banners, demos, or CMS content.
/// </summary>
public static class DbInitializer
{
    private const int MaxAttempts = 12;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private const string DevDefaultAdminEmail = "admin@hondahieunga.vn";
    private const string DevDefaultAdminPassword = "Admin@123456!";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HieuNgaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<HieuNgaDbContext>>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var seedOptions = configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();

        await ExecuteWithRetryAsync(
            async () => await context.Database.MigrateAsync(),
            "Applying database migrations",
            logger);

        await SeedAdminUserAsync(scope.ServiceProvider, environment, seedOptions, logger);
    }

    private static async Task ExecuteWithRetryAsync(Func<Task> action, string description, ILogger logger)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransientDbError(ex))
            {
                logger.LogWarning(ex, "{Description} failed (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    description, attempt, MaxAttempts, RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay);
            }
        }
    }

    private static bool IsTransientDbError(Exception ex)
    {
        var message = ex.ToString();
        return message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase)
               || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Exception while reading from stream", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SeedAdminUserAsync(
        IServiceProvider sp,
        IHostEnvironment environment,
        SeedOptions seedOptions,
        ILogger logger)
    {
        if (!environment.IsDevelopment() && !seedOptions.AdminSeedEnabled)
        {
            logger.LogInformation(
                "Admin seed skipped: set SeedOptions__AdminSeedEnabled=true (or AdminSeed__Enabled=true) with email/password on first deploy.");
            return;
        }

        var email = !string.IsNullOrWhiteSpace(seedOptions.AdminEmail)
            ? seedOptions.AdminEmail.Trim()
            : environment.IsDevelopment() ? DevDefaultAdminEmail : null;

        var password = !string.IsNullOrWhiteSpace(seedOptions.AdminPassword)
            ? seedOptions.AdminPassword
            : environment.IsDevelopment() ? DevDefaultAdminPassword : null;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
            {
                logger.LogWarning(
                    "Admin seed skipped: set SeedOptions__AdminEmail and SeedOptions__AdminPassword (12+ chars) before first production deploy.");
            }
            return;
        }

        if (!environment.IsDevelopment() && password.Length < 12)
        {
            logger.LogWarning("Admin seed skipped: production admin password must be at least 12 characters.");
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var result = await userManager.CreateAsync(new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Administrator",
            EmailConfirmed = true
        }, password);

        if (result.Succeeded)
            logger.LogInformation("Admin user seeded for {Email}.", email);
        else
            logger.LogWarning("Admin user seed failed for {Email}: {Errors}", email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
