using HieuNga.Domain.Enums;

namespace HieuNga.Application.DemoImport;

public sealed class DemoMotorcycleMetadata
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Category { get; set; } = "Scooter";
    public decimal Price { get; set; }
    public bool Featured { get; set; }
    public bool Published { get; set; } = true;
    public int SortOrder { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionHtml { get; set; }
    public int? EngineCc { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public List<string> Highlights { get; set; } = [];
    public List<DemoSpecItem> Specifications { get; set; } = [];
    public List<DemoVariantItem> Variants { get; set; } = [];
    public List<DemoColorItem> Colors { get; set; } = [];
    public List<DemoContentCard> Features { get; set; } = [];
    public List<DemoContentCard> Technology { get; set; } = [];
    public DemoSeoMetadata Seo { get; set; } = new();
    public DemoFinanceDefaults Finance { get; set; } = new();
    public DemoAssetHints Assets { get; set; } = new();
}

public sealed class DemoSpecItem
{
    public string Icon { get; set; } = "•";
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed class DemoVariantItem
{
    public string Name { get; set; } = "Tiêu chuẩn";
    public decimal? Price { get; set; }
    public int StockQuantity { get; set; } = 5;
    public bool IsAvailable { get; set; } = true;
    public string? Sku { get; set; }
}

public sealed class DemoColorItem
{
    public string Name { get; set; } = "";
    public string Hex { get; set; } = "#000000";
    /// <summary>File name under colors/ (e.g. black.jpg). Optional — importer also matches by sanitized color name.</summary>
    public string? Image { get; set; }
}

public sealed class DemoContentCard
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    /// <summary>Optional image file under features/ or technology/.</summary>
    public string? Image { get; set; }
}

public sealed class DemoSeoMetadata
{
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
}

public sealed class DemoFinanceDefaults
{
    public bool CalculatorEnabled { get; set; } = true;
    public string? DefaultBankId { get; set; }
    public decimal DefaultDownPaymentPercent { get; set; } = 20;
    public int DefaultTermMonths { get; set; } = 12;
}

public sealed class DemoAssetHints
{
    public string Thumbnail { get; set; } = "thumbnail.jpg";
    public string GalleryFolder { get; set; } = "gallery";
    public string SpinFolder { get; set; } = "360";
    public string ColorsFolder { get; set; } = "colors";
    public string FeaturesFolder { get; set; } = "features";
    public string TechnologyFolder { get; set; } = "technology";
}

public sealed record DemoPackageInfo(
    string PackageId,
    string DisplayName,
    string FolderName,
    bool HasMetadata,
    bool IsImported,
    Guid? MotorcycleId,
    string? Slug,
    string? ThumbnailPreviewUrl,
    string StatusLabel);

public sealed record DemoImportResult(
    bool Success,
    string Message,
    Guid? MotorcycleId,
    string? Slug,
    int UploadedImages,
    IReadOnlyList<string> Warnings);

public sealed record DemoCatalogSeedResult(
    bool Success,
    string Message,
    int Created,
    int Updated,
    int Skipped,
    int UploadedImages,
    IReadOnlyDictionary<string, int> CountsByCategory,
    IReadOnlyList<string> Warnings);

public interface IDemoMotorcycleImporter
{
    string AssetsRootPath { get; }
    bool StorageReady { get; }
    string StorageDescription { get; }

    Task<IReadOnlyList<DemoPackageInfo>> ListPackagesAsync(CancellationToken ct = default);
    Task<DemoImportResult> ImportAsync(string packageId, CancellationToken ct = default);
    Task<DemoImportResult> DeleteDemoAsync(string packageId, CancellationToken ct = default);
    Task<DemoCatalogSeedResult> SeedFullCatalogAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetPublishedCategoryCountsAsync(CancellationToken ct = default);
}

public static class DemoPackageCatalog
{
    public static IReadOnlyList<(string Id, string DisplayName, string Folder)> All { get; } =
    [
        ("vision", "Vision", "Vision"),
        ("lead", "Lead", "Lead"),
        ("airblade", "Air Blade", "AirBlade"),
        ("sh", "SH", "SH"),
        ("winnerx", "Winner X", "WinnerX"),
        ("future", "Future", "Future"),
        ("wavealpha", "Wave Alpha", "WaveAlpha")
    ];

    public static MotorcycleCategory ParseCategory(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return MotorcycleCategory.Scooter;
        var key = raw.Trim().ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        return key switch
        {
            "scooter" => MotorcycleCategory.Scooter,
            "xeso" or "so" => MotorcycleCategory.XeSo,
            "contay" or "xecontay" or "con" => MotorcycleCategory.ConTay,
            "phankhoilon" or "xephankhoilon" or "bigbike" or "pkl" => MotorcycleCategory.PhanKhoiLon,
            "electric" or "xedien" or "ev" => MotorcycleCategory.Electric,
            _ => Enum.TryParse<MotorcycleCategory>(raw, true, out var parsed)
                ? parsed
                : MotorcycleCategory.Scooter
        };
    }
}
