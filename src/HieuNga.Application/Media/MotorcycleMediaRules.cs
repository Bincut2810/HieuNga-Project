using HieuNga.Application.DTOs;

namespace HieuNga.Application.Media;

/// <summary>Single hero resolution rule for motorcycle detail (color → thumbnail → placeholder).</summary>
public static class MotorcycleMediaRules
{
    public const string PlaceholderImage = "/images/motorcycles/default.svg";

    public static string ResolveHero(IEnumerable<string?> colorImageUrls, string? thumbnailUrl)
    {
        foreach (var url in colorImageUrls)
        {
            if (!string.IsNullOrWhiteSpace(url)) return url!;
        }
        if (!string.IsNullOrWhiteSpace(thumbnailUrl)) return thumbnailUrl!;
        return PlaceholderImage;
    }

    public static string ResolveHero(IReadOnlyList<MotorcycleColorDto> colors, string? thumbnailUrl) =>
        ResolveHero(colors.Select(c => c.ImageUrl), thumbnailUrl);
}
