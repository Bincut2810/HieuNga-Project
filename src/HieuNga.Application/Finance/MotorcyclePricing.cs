using HieuNga.Application.DTOs;

namespace HieuNga.Application.Finance;

/// <summary>Single source of truth for motorcycle selling price used by finance and listings.</summary>
public static class MotorcyclePricing
{
    /// <summary>
    /// EffectivePrice = first positive variant price; otherwise BasePrice.
    /// Zero-priced variants never shadow a positive BasePrice.
    /// </summary>
    public static decimal ResolveEffectivePrice(decimal basePrice, IEnumerable<decimal> variantPrices)
    {
        foreach (var price in variantPrices)
        {
            if (price > 0m) return price;
        }

        return basePrice > 0m ? basePrice : 0m;
    }

    public static decimal ResolveEffectivePrice(decimal basePrice, IEnumerable<MotorcycleVariantDto> variants) =>
        ResolveEffectivePrice(basePrice, variants.Select(v => v.Price));
}
