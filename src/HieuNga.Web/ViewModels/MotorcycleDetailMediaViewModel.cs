using System.Text.Json;
using System.Text.Json.Serialization;
using HieuNga.Application.DTOs;

namespace HieuNga.Web.ViewModels;

/// <summary>
/// Sole media presentation model for the public motorcycle detail page.
/// Built once from <see cref="MotorcycleDetailDto"/> — Razor must not remap images.
/// </summary>
public sealed class MotorcycleDetailMediaViewModel
{
    public const string PlaceholderImage = "/images/motorcycles/default.svg";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public required string Name { get; init; }
    public required string HeroImage { get; init; }
    public required IReadOnlyList<string> Gallery { get; init; }
    public required IReadOnlyList<MotorcycleAngleImageDto> Angles { get; init; }
    public required IReadOnlyList<MediaColorItem> Colors { get; init; }
    public required string DefaultImage { get; init; }
    public required string MediaJson { get; init; }

    public bool HasGallery => Gallery.Count > 0;
    public bool HasAngles => Angles.Count > 0;
    public bool ShowAngleViewer => Angles.Count >= 2;
    public bool HasColors => Colors.Count > 0;

    public sealed record MediaColorItem(string Id, string Name, string Hex, string? ImageUrl);

    public static MotorcycleDetailMediaViewModel FromDto(MotorcycleDetailDto dto)
    {
        var gallery = dto.GalleryUrls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var angles = dto.AngleImages
            .Where(a => !string.IsNullOrWhiteSpace(a.Url))
            .ToList();

        var colors = dto.Colors
            .Select(c => new MediaColorItem(
                c.Id.ToString(),
                c.Name,
                c.HexCode,
                string.IsNullOrWhiteSpace(c.ImageUrl) ? null : c.ImageUrl))
            .ToList();

        // CMS hero/thumb only — no gallery invent. Placeholder when Admin left both empty.
        var hero = FirstNonEmpty(dto.HeroImageUrl, dto.ThumbnailUrl) ?? PlaceholderImage;

        var payload = new
        {
            hero,
            gallery,
            angles = angles.Select(a => new { a.Angle, a.Label, a.Url }),
            colors = colors.Select(c => new { c.Id, c.Name, hex = c.Hex, imageUrl = c.ImageUrl }),
            name = dto.Name,
            storageKey = $"hn-color-{dto.Id}"
        };

        return new MotorcycleDetailMediaViewModel
        {
            Name = dto.Name,
            HeroImage = hero,
            Gallery = gallery,
            Angles = angles,
            Colors = colors,
            DefaultImage = PlaceholderImage,
            MediaJson = JsonSerializer.Serialize(payload, JsonOptions)
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
        return null;
    }
}
