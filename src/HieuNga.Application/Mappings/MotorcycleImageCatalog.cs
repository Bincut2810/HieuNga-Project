using HieuNga.Application.Mappings;

namespace HieuNga.Application.Mappings;

/// <summary>Fallback static SVGs when a motorcycle has no uploaded media.</summary>
public static class MotorcycleImageCatalog
{
    public const string Default = "/images/motorcycles/default.svg";

    public static string GetFallbackThumbnail(string? slug) => slug?.ToLowerInvariant() switch
    {
        "honda-vision-2025" => "/images/motorcycles/honda-vision-2025.svg",
        "honda-sh-160i" => "/images/motorcycles/honda-sh-160i.svg",
        "honda-winner-x" => "/images/motorcycles/honda-winner-x.svg",
        "honda-cb150r" => "/images/motorcycles/honda-cb150r.svg",
        _ => Default
    };

    public static bool IsValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)) return true;
        if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return true;
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Prefer real CMS URLs; fall back to static SVG only when empty/invalid.</summary>
    public static string ResolveThumbnail(string slug, string? thumbnailUrl, string? firstMediaUrl = null)
    {
        if (IsValidImageUrl(thumbnailUrl)) return thumbnailUrl!;
        if (IsValidImageUrl(firstMediaUrl)) return firstMediaUrl!;
        return GetFallbackThumbnail(slug);
    }

    public static IReadOnlyList<string> ResolveGallery(string slug, IEnumerable<string?> urls)
    {
        var list = urls
            .Where(IsValidImageUrl)
            .Select(u => u!)
            .Distinct()
            .ToList();

        if (list.Count > 0) return list;
        var fallback = GetFallbackThumbnail(slug);
        return [fallback];
    }

    // Back-compat aliases used by seed/enricher
    public static readonly HashSet<string> DemoSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "honda-vision-2025", "honda-sh-160i", "honda-winner-x", "honda-cb150r"
    };

    public static string GetThumbnail(string slug) => GetFallbackThumbnail(slug);
    public static string GetGalleryPrimary(string slug) => GetFallbackThumbnail(slug);
}
