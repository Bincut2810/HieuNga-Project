using HieuNga.Application.Mappings;
using HieuNga.Domain.Entities;

namespace HieuNga.Tests;

public class BannerHeroDtoTests
{
    [Fact]
    public void ToDto_Maps_All_Banner_Fields()
    {
        var id = Guid.NewGuid();
        var banner = new Banner
        {
            Id = id,
            Title = "Hero title",
            Subtitle = "Hero sub",
            ImageUrl = "/desk.jpg",
            PrimaryButtonText = "Primary",
            PrimaryButtonUrl = "/xe",
            SortOrder = 2,
            IsActive = true
        };

        var dto = banner.ToDto();

        Assert.Equal(id, dto.Id);
        Assert.Equal("Hero title", dto.Title);
        Assert.Equal("Hero sub", dto.Subtitle);
        Assert.Equal("/desk.jpg", dto.ImageUrl);
        Assert.Equal("Primary", dto.PrimaryButtonText);
        Assert.Equal("/xe", dto.PrimaryButtonUrl);
        Assert.Equal(2, dto.DisplayOrder);
        Assert.True(dto.Enabled);
    }

    [Fact]
    public void BannerDto_Defaults_Match_Entity_Defaults()
    {
        var dto = new Banner { Title = "t", ImageUrl = "/i.jpg" }.ToDto();

        Assert.Equal(0, dto.DisplayOrder);
        Assert.True(dto.Enabled);
        Assert.Null(dto.Subtitle);
        Assert.Null(dto.PrimaryButtonText);
        Assert.Null(dto.PrimaryButtonUrl);
    }
}
