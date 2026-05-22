namespace HieuNga.Application.Mappings;

/// <summary>Canonical demo motorcycle imagery — local static files (demo-stable, no CDN failures).</summary>
public static class MotorcycleImageCatalog
{
    public static readonly HashSet<string> DemoSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "honda-vision-2025",
        "honda-sh-160i",
        "honda-winner-x",
        "honda-cb150r"
    };

    public const string Default = "/images/motorcycles/default.jpg";

    public static string GetThumbnail(string slug) => slug.ToLowerInvariant() switch
    {
        "honda-vision-2025" => "/images/motorcycles/honda-vision-2025.jpg",
        "honda-sh-160i" => "/images/motorcycles/honda-sh-160i.jpg",
        "honda-winner-x" => "/images/motorcycles/honda-winner-x.jpg",
        "honda-cb150r" => "/images/motorcycles/honda-cb150r.jpg",
        _ => Default
    };

    public static string GetGalleryPrimary(string slug) => GetThumbnail(slug);

    public static bool IsValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (url.StartsWith("/images/motorcycles/", StringComparison.OrdinalIgnoreCase)) return true;
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Demo inventory always uses local catalog URLs (ignores broken DB/CDN links).</summary>
    public static string ResolveThumbnail(string slug, string? thumbnailUrl, string? firstMediaUrl = null)
    {
        if (DemoSlugs.Contains(slug))
            return GetThumbnail(slug);

        if (IsValidImageUrl(thumbnailUrl)) return thumbnailUrl!;
        if (IsValidImageUrl(firstMediaUrl)) return firstMediaUrl!;
        return GetThumbnail(slug);
    }

    public static IReadOnlyList<string> ResolveGallery(string slug, IEnumerable<string?> urls)
    {
        if (DemoSlugs.Contains(slug))
        {
            var primary = GetGalleryPrimary(slug);
            return [primary, primary, GetThumbnail(slug)];
        }

        var list = urls
            .Where(IsValidImageUrl)
            .Select(u => u!)
            .Distinct()
            .Take(8)
            .ToList();

        if (list.Count > 0) return list;

        return [GetGalleryPrimary(slug), GetThumbnail(slug)];
    }
}
