using HieuNga.Application.DTOs;
using HieuNga.Application.Mappings;
using HieuNga.Domain.Entities;

namespace HieuNga.Tests;

public class BannerHeroDtoTests
{
    [Fact]
    public void ToHomepageHero_Maps_Slides_And_Shared_Text()
    {
        var banners = new List<Banner>
        {
            new()
            {
                Title = "Hero title",
                Subtitle = "Hero sub",
                ImageUrl = "/a.jpg",
                SortOrder = 0,
                IsActive = true
            },
            new()
            {
                Title = "Hero title",
                Subtitle = "Hero sub",
                ImageUrl = "/b.jpg",
                SortOrder = 1,
                IsActive = true
            }
        };

        var hero = banners.ToHomepageHero();

        Assert.Equal("Hero title", hero.Title);
        Assert.Equal("Hero sub", hero.Subtitle);
        Assert.True(hero.Enabled);
        Assert.Equal(2, hero.Slides.Count);
        Assert.Equal("/a.jpg", hero.Slides[0].ImageUrl);
        Assert.Equal("/b.jpg", hero.Slides[1].ImageUrl);
    }

    [Fact]
    public void ToHomepageHero_Empty_Returns_Disabled_Hero()
    {
        var hero = Array.Empty<Banner>().ToHomepageHero();

        Assert.Equal("", hero.Title);
        Assert.Null(hero.Subtitle);
        Assert.False(hero.Enabled);
        Assert.Empty(hero.Slides);
    }
}
