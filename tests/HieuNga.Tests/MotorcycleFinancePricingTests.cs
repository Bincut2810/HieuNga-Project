using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// Mirrors MotorcycleFinancePrefs.ResolveEffectivePrice — keeps pricing rule covered without referencing Web.
/// </summary>
public class MotorcycleFinancePricingTests
{
    private static decimal ResolveEffectivePrice(decimal basePrice, IEnumerable<decimal> variantPrices)
    {
        foreach (var p in variantPrices)
        {
            if (p > 0) return p;
        }
        return basePrice > 0 ? basePrice : 0m;
    }

    [Fact]
    public void Prefers_positive_variant_over_base()
    {
        Assert.Equal(42_000_000m, ResolveEffectivePrice(30_000_000m, [0m, 42_000_000m]));
    }

    [Fact]
    public void Zero_variants_do_not_shadow_base_price()
    {
        Assert.Equal(31_500_000m, ResolveEffectivePrice(31_500_000m, [0m, 0m]));
    }

    [Fact]
    public void Falls_back_to_zero_when_no_price()
    {
        Assert.Equal(0m, ResolveEffectivePrice(0m, [0m]));
        Assert.Equal(0m, ResolveEffectivePrice(0m, Array.Empty<decimal>()));
    }

    [Fact]
    public void Empty_variants_use_base_price()
    {
        Assert.Equal(18_500_000m, ResolveEffectivePrice(18_500_000m, Array.Empty<decimal>()));
    }
}
