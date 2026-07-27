using HieuNga.Application.Mappings;
using HieuNga.Domain;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;

namespace HieuNga.Tests;

public class AngleImagesMappingTests
{
    [Fact]
    public void ToDetail_AngleImages_match_cms_catalog_order_and_keys()
    {
        var id = Guid.NewGuid();
        var bike = new Motorcycle
        {
            Id = id,
            Name = "Test",
            Slug = "test-bike",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1,
            SpinFrames =
            [
                // Deliberately out of order + duplicate angle (newer wins)
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.Rear, ImageUrl = "https://cdn.example/rear-old.jpg", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.FrontRight, ImageUrl = "https://cdn.example/fr.jpg" },
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.Front, ImageUrl = "https://cdn.example/front.jpg" },
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.Left, ImageUrl = "https://cdn.example/left.jpg" },
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.FrontLeft, ImageUrl = "https://cdn.example/fl.jpg" },
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.Right, ImageUrl = "https://cdn.example/right.jpg" },
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.Rear, ImageUrl = "https://cdn.example/rear.jpg", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            ]
        };

        var dto = bike.ToDetail();

        Assert.Equal(6, dto.AngleImages.Count);
        Assert.Equal(
            MotorcycleViewAngleCatalog.All.Select(e => e.Key).ToArray(),
            dto.AngleImages.Select(a => a.Angle).ToArray());
        Assert.Equal(
            MotorcycleViewAngleCatalog.All.Select(e => e.LabelVi).ToArray(),
            dto.AngleImages.Select(a => a.Label).ToArray());
        Assert.Equal("https://cdn.example/rear.jpg", dto.AngleImages.Single(a => a.Angle == "rear").Url);
        Assert.DoesNotContain(dto.AngleImages, a => a.Url.Contains("rear-old"));
    }

    [Fact]
    public void ToDetail_AngleImages_omit_empty_slots()
    {
        var id = Guid.NewGuid();
        var bike = new Motorcycle
        {
            Id = id,
            Name = "Partial",
            Slug = "partial",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1,
            SpinFrames =
            [
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.Front, ImageUrl = "https://cdn.example/front.jpg" },
                new MotorcycleSpinFrame { MotorcycleId = id, Angle = MotorcycleViewAngle.Left, ImageUrl = "https://cdn.example/left.jpg" },
            ]
        };

        var dto = bike.ToDetail();
        Assert.Equal(2, dto.AngleImages.Count);
        Assert.Equal(["front", "left"], dto.AngleImages.Select(a => a.Angle).ToArray());
    }
}
