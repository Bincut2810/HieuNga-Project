namespace HieuNga.Infrastructure.Persistence;

/// <summary>
/// Optional first-deploy admin bootstrap. Override via appsettings or SeedOptions__* / AdminSeed__*.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "SeedOptions";

    /// <summary>Admin email for initial seed when no admin exists.</summary>
    public string? AdminEmail { get; set; }

    /// <summary>Admin password for initial seed. Required in production (12+ chars).</summary>
    public string? AdminPassword { get; set; }

    /// <summary>
    /// When true in non-Development environments, allows creating the initial admin account from env vars.
    /// Alias: AdminSeed__Enabled maps to this property.
    /// </summary>
    public bool AdminSeedEnabled { get; set; }
}
