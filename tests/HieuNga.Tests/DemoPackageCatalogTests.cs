using HieuNga.Application.DemoImport;
using HieuNga.Domain.Enums;

namespace HieuNga.Tests;

public class DemoPackageCatalogTests
{
    [Theory]
    [InlineData("Scooter", MotorcycleCategory.Scooter)]
    [InlineData("xe-so", MotorcycleCategory.XeSo)]
    [InlineData("ConTay", MotorcycleCategory.ConTay)]
    [InlineData("pkl", MotorcycleCategory.PhanKhoiLon)]
    [InlineData("electric", MotorcycleCategory.Electric)]
    public void ParseCategory_maps_known_labels(string raw, MotorcycleCategory expected)
    {
        Assert.Equal(expected, DemoPackageCatalog.ParseCategory(raw));
    }

    [Fact]
    public void Catalog_includes_vision_package()
    {
        Assert.Contains(DemoPackageCatalog.All, p => p.Folder == "Vision" && p.Id == "vision");
    }
}
