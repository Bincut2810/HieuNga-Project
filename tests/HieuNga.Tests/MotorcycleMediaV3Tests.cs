using HieuNga.Application.DTOs;
using HieuNga.Application.Mappings;
using HieuNga.Application.Media;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;

namespace HieuNga.Tests;

public class MotorcycleMediaV3Tests
{
    [Fact]
    public void ToDetail_has_no_gallery_or_hero_fields()
    {
        var props = typeof(MotorcycleDetailDto).GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("HeroImageUrl", props);
        Assert.DoesNotContain("GalleryUrls", props);
        Assert.Contains("ThumbnailUrl", props);
        Assert.Contains("Colors", props);
        Assert.Contains("AngleImages", props);
    }

    [Fact]
    public void ToListItem_uses_thumbnail_only()
    {
        var bike = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Vision",
            Slug = "vision",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1,
            ThumbnailUrl = "https://cdn.example/thumb.jpg",
            Colors =
            [
                new MotorcycleColor { Name = "Đỏ", HexCode = "#F00", ImageUrl = "https://cdn.example/red.jpg" }
            ]
        };

        var item = bike.ToListItem();
        Assert.Equal("https://cdn.example/thumb.jpg", item.ThumbnailUrl);
    }

    [Fact]
    public void ResolveHero_prefers_first_color_image()
    {
        var colors = new List<MotorcycleColorDto>
        {
            new(Guid.NewGuid(), "Trắng", "#FFF", "https://cdn.example/white.jpg"),
            new(Guid.NewGuid(), "Đen", "#111", "https://cdn.example/black.jpg")
        };
        var hero = MotorcycleMediaRules.ResolveHero(colors, "https://cdn.example/thumb.jpg");
        Assert.Equal("https://cdn.example/white.jpg", hero);
    }

    [Fact]
    public void ResolveHero_falls_back_to_thumbnail_when_colors_have_no_image()
    {
        var colors = new List<MotorcycleColorDto>
        {
            new(Guid.NewGuid(), "Trắng", "#FFF", null)
        };
        var hero = MotorcycleMediaRules.ResolveHero(colors, "https://cdn.example/thumb.jpg");
        Assert.Equal("https://cdn.example/thumb.jpg", hero);
    }

    [Fact]
    public void ResolveHero_uses_placeholder_when_empty()
    {
        var hero = MotorcycleMediaRules.ResolveHero(Array.Empty<string?>(), null);
        Assert.Equal(MotorcycleMediaRules.PlaceholderImage, hero);
    }
}
