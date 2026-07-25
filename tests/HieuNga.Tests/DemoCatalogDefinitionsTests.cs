using HieuNga.Application.DemoImport;
using HieuNga.Domain.Enums;

namespace HieuNga.Tests;

public class DemoCatalogDefinitionsTests
{
    [Fact]
    public void Catalog_has_five_bikes_per_category()
    {
        var groups = DemoCatalogDefinitions.All
            .GroupBy(m => DemoPackageCatalog.ParseCategory(m.Category))
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(5, groups[MotorcycleCategory.Scooter]);
        Assert.Equal(5, groups[MotorcycleCategory.XeSo]);
        Assert.Equal(5, groups[MotorcycleCategory.ConTay]);
        Assert.Equal(5, groups[MotorcycleCategory.PhanKhoiLon]);
        Assert.Equal(5, groups[MotorcycleCategory.Electric]);
        Assert.Equal(25, DemoCatalogDefinitions.All.Count);
    }

    [Fact]
    public void Catalog_slugs_are_unique_and_demo_prefixed()
    {
        var slugs = DemoCatalogDefinitions.All.Select(m => m.Slug).ToList();
        Assert.Equal(slugs.Count, slugs.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(slugs, s => Assert.StartsWith("demo-", s, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Catalog_items_are_published_with_finance_defaults()
    {
        Assert.All(DemoCatalogDefinitions.All, m =>
        {
            Assert.True(m.Published);
            Assert.True(m.Finance.CalculatorEnabled);
            Assert.False(string.IsNullOrWhiteSpace(m.Name));
            Assert.NotEmpty(m.Specifications);
            Assert.NotEmpty(m.Colors);
        });
    }
}
