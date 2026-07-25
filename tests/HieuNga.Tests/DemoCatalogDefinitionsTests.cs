using HieuNga.Application.Catalog;
using HieuNga.Application.DemoImport;
using HieuNga.Domain.Enums;

namespace HieuNga.Tests;

public class DemoCatalogDefinitionsTests
{
    [Fact]
    public void Catalog_matches_inventory_targets()
    {
        var groups = DemoCatalogDefinitions.All
            .GroupBy(m => DemoPackageCatalog.ParseCategory(m.Category))
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(HieuNgaInventoryTargets.Targets[MotorcycleCategory.Scooter], groups[MotorcycleCategory.Scooter]);
        Assert.Equal(HieuNgaInventoryTargets.Targets[MotorcycleCategory.XeSo], groups[MotorcycleCategory.XeSo]);
        Assert.Equal(HieuNgaInventoryTargets.Targets[MotorcycleCategory.ConTay], groups[MotorcycleCategory.ConTay]);
        Assert.Equal(HieuNgaInventoryTargets.Targets[MotorcycleCategory.PhanKhoiLon], groups[MotorcycleCategory.PhanKhoiLon]);
        Assert.Equal(HieuNgaInventoryTargets.Targets[MotorcycleCategory.Electric], groups[MotorcycleCategory.Electric]);
        Assert.Equal(HieuNgaInventoryTargets.Targets.Values.Sum(), DemoCatalogDefinitions.All.Count);
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
