using HieuNga.Application.Mappings;

namespace HieuNga.Application.Mappings;

/// <summary>
/// CMS image URL helpers for motorcycles.
/// Public mapping must not invent Unsplash/demo assets — seed/enricher may still use slug SVGs.
/// </summary>
public static class MotorcycleImageCatalog
{
    /// <summary>Presentation broken-image recovery only — not a CMS substitute.</summary>
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

    /// <summary>CMS thumbnail only — thumb, else first gallery asset. Null when Admin has no image.</summary>
    public static string? ResolveThumbnail(string? thumbnailUrl, string? firstMediaUrl = null)
    {
        if (IsValidImageUrl(thumbnailUrl)) return thumbnailUrl!;
        if (IsValidImageUrl(firstMediaUrl)) return firstMediaUrl!;
        return null;
    }

    /// <summary>Valid CMS gallery URLs only — empty when none (no invent).</summary>
    public static IReadOnlyList<string> ResolveGallery(IEnumerable<string?> urls)
    {
        return urls
            .Where(IsValidImageUrl)
            .Select(u => u!)
            .Distinct()
            .ToList();
    }

    /// <summary>Seed/enricher: pad empty gallery with slug SVG (not used by public ToDetail).</summary>
    public static IReadOnlyList<string> ResolveGalleryOrSeedFallback(string slug, IEnumerable<string?> urls)
    {
        var list = ResolveGallery(urls);
        if (list.Count > 0) return list;
        return [GetFallbackThumbnail(slug)];
    }

    public static readonly HashSet<string> DemoSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "honda-vision-2025", "honda-sh-160i", "honda-winner-x", "honda-cb150r"
    };

    public static string GetThumbnail(string slug) => GetFallbackThumbnail(slug);
    public static string GetGalleryPrimary(string slug) => GetFallbackThumbnail(slug);
}
