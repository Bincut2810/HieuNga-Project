using HieuNga.Application.Mappings;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;

namespace HieuNga.Tests;

public class BannerHeroDtoTests
{
    [Fact]
    public void ToDto_Maps_Premium_Hero_Fields()
    {
        var id = Guid.NewGuid();
        var banner = new Banner
        {
            Id = id,
            Title = "Hero title",
            Subtitle = "Hero sub",
            ImageUrl = "/desk.jpg",
            MobileImageUrl = "/mob.jpg",
            CtaText = "Primary",
            CtaUrl = "/xe",
            SecondaryCtaText = "Secondary",
            SecondaryCtaUrl = "/tra-gop",
            Badge = "HEAD Đà Nẵng",
            OverlayStrength = 72,
            TextAlignment = BannerTextAlignment.Center
        };

        var dto = banner.ToDto();

        Assert.Equal(id, dto.Id);
        Assert.Equal("Hero title", dto.Title);
        Assert.Equal("Hero sub", dto.Subtitle);
        Assert.Equal("/desk.jpg", dto.ImageUrl);
        Assert.Equal("/mob.jpg", dto.MobileImageUrl);
        Assert.Equal("Primary", dto.CtaText);
        Assert.Equal("/xe", dto.CtaUrl);
        Assert.Equal("Secondary", dto.SecondaryCtaText);
        Assert.Equal("/tra-gop", dto.SecondaryCtaUrl);
        Assert.Equal("HEAD Đà Nẵng", dto.Badge);
        Assert.Equal(72, dto.OverlayStrength);
        Assert.Equal(BannerTextAlignment.Center, dto.TextAlignment);
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(0, 0)]
    [InlineData(65, 65)]
    [InlineData(100, 100)]
    [InlineData(140, 100)]
    public void ToDto_Clamps_OverlayStrength(int input, int expected)
    {
        var dto = new Banner
        {
            Title = "t",
            ImageUrl = "/i.jpg",
            OverlayStrength = input
        }.ToDto();

        Assert.Equal(expected, dto.OverlayStrength);
    }

    [Fact]
    public void BannerDto_Defaults_Match_Entity_Defaults()
    {
        var dto = new Banner { Title = "t", ImageUrl = "/i.jpg" }.ToDto();
        Assert.Equal(65, dto.OverlayStrength);
        Assert.Equal(BannerTextAlignment.Left, dto.TextAlignment);
        Assert.Null(dto.SecondaryCtaUrl);
        Assert.Null(dto.Badge);
    }
}
