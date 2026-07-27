using System.Text.Json;
using System.Text.Json.Serialization;
using HieuNga.Application.DTOs;
using HieuNga.Application.Media;

namespace HieuNga.Web.ViewModels;

/// <summary>
/// Sole media presentation model for public motorcycle detail.
/// Hero = selected color image → first color image → thumbnail → default.svg
/// </summary>
public sealed class MotorcycleDetailMediaViewModel
{
    public const string PlaceholderImage = MotorcycleMediaRules.PlaceholderImage;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public required string Name { get; init; }
    public required string Thumbnail { get; init; }
    /// <summary>Initial hero for SSR (first color with image, else thumbnail, else placeholder).</summary>
    public required string HeroImage { get; init; }
    public required IReadOnlyList<MotorcycleAngleImageDto> Angles { get; init; }
    public required IReadOnlyList<MediaColorItem> Colors { get; init; }
    public required string DefaultImage { get; init; }
    public required string MediaJson { get; init; }

    public bool HasAngles => Angles.Count > 0;
    public bool ShowAngleViewer => Angles.Count >= 2;
    public bool HasColors => Colors.Count > 0;

    public sealed record MediaColorItem(string Id, string Name, string Hex, string? ImageUrl);

    public static MotorcycleDetailMediaViewModel FromDto(MotorcycleDetailDto dto)
    {
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

        var thumbnail = string.IsNullOrWhiteSpace(dto.ThumbnailUrl)
            ? PlaceholderImage
            : dto.ThumbnailUrl!;
        var hero = MotorcycleMediaRules.ResolveHero(dto.Colors, dto.ThumbnailUrl);

        var payload = new
        {
            thumbnail,
            colors = colors.Select(c => new { c.Id, c.Name, hex = c.Hex, imageUrl = c.ImageUrl }),
            angles = angles.Select(a => new { a.Angle, a.Label, a.Url }),
            name = dto.Name,
            storageKey = $"hn-color-{dto.Id}"
        };

        return new MotorcycleDetailMediaViewModel
        {
            Name = dto.Name,
            Thumbnail = thumbnail,
            HeroImage = hero,
            Angles = angles,
            Colors = colors,
            DefaultImage = PlaceholderImage,
            MediaJson = JsonSerializer.Serialize(payload, JsonOptions)
        };
    }
}
