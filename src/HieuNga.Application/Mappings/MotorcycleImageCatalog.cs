namespace HieuNga.Application.Mappings;

/// <summary>CMS image URL helpers for motorcycles. Listing uses Thumbnail only.</summary>
public static class MotorcycleImageCatalog
{
    /// <summary>Presentation broken-image recovery only — not a CMS substitute.</summary>
    public const string Default = "/images/motorcycles/default.svg";

    public static bool IsValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)) return true;
        if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return true;
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>CMS thumbnail only. Null when Admin has no image.</summary>
    public static string? ResolveThumbnail(string? thumbnailUrl) =>
        IsValidImageUrl(thumbnailUrl) ? thumbnailUrl : null;
}
