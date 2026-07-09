namespace HieuNga.Infrastructure.Persistence;

/// <summary>
/// Database seed behavior. Override via appsettings or environment variables (SeedOptions__*).
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "SeedOptions";

    /// <summary>Admin email for initial seed when no admin exists.</summary>
    public string? AdminEmail { get; set; }

    /// <summary>Admin password for initial seed. Required in production (12+ chars).</summary>
    public string? AdminPassword { get; set; }

    /// <summary>
    /// When true, runs MotorcycleContentEnricher on startup (overwrites demo motorcycle fields).
    /// Default: enabled only in Development via DbInitializer.
    /// </summary>
    public bool RunContentEnricher { get; set; }

    /// <summary>
    /// When true in non-Development environments, allows creating the initial admin account from env vars.
    /// Alias: AdminSeed__Enabled maps to this property.
    /// </summary>
    public bool AdminSeedEnabled { get; set; }

    /// <summary>
    /// When true, seeds demo motorcycles, banners, promotions, and blog posts on empty database.
    /// Default: Development only. Set true for one-time staging demo seed.
    /// </summary>
    public bool EnableDemoSeed { get; set; }
}
