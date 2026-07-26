using HieuNga.Application.DTOs;
using HieuNga.Application.Finance;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Xe;

public class ChiTietModel(
    IMotorcycleService motorcycleService,
    IFinanceConfigService financeConfig) : PageModel
{
    public MotorcycleDetailDto? Motorcycle { get; private set; }
    public IReadOnlyList<MotorcycleListItemDto> Related { get; private set; } = [];
    public FinanceCalculatorViewModel Finance { get; private set; } = FinanceCalculatorViewModel.Create(0, []);
    public bool IsAvailable { get; private set; } = true;
    public string AvailabilityLabel { get; private set; } = "Còn hàng";
    public string? FuelConsumption { get; private set; }
    public string? Warranty { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        Motorcycle = await motorcycleService.GetBySlugAsync(slug, ct);
        if (Motorcycle is null) return NotFound();

        ResolveAvailability(Motorcycle);
        FuelConsumption = FindSpecValue(Motorcycle.Specifications, "tiêu hao", "mức tiêu thụ", "l/100", "km/lít", "km/l");
        Warranty = FindSpecValue(Motorcycle.Specifications, "bảo hành", "warranty");

        var price = MotorcyclePricing.ResolveEffectivePrice(Motorcycle.BasePrice, Motorcycle.Variants);
        var banks = await financeConfig.GetActiveBanksAsync(ct);
        Finance = FinanceCalculatorViewModel.Create(price, banks);

        Related = await motorcycleService.GetRelatedAsync(Motorcycle.Id, ct);

        ViewData["HideDefaultMobileCta"] = true;
        this.SetSeo(Motorcycle.Seo, $"{Motorcycle.Name} | Xe Máy Hiếu Nga", Motorcycle.ShortDescription);
        return Page();
    }

    private void ResolveAvailability(MotorcycleDetailDto m)
    {
        if (m.Variants.Count == 0)
        {
            IsAvailable = true;
            AvailabilityLabel = "Còn hàng";
            return;
        }

        IsAvailable = m.Variants.Any(v => v.IsAvailable);
        AvailabilityLabel = IsAvailable ? "Còn hàng" : "Hết hàng";
    }

    private static string? FindSpecValue(IReadOnlyList<MotorcycleSpecItemDto> specs, params string[] needles)
    {
        foreach (var spec in specs)
        {
            if (string.Equals(spec.Icon, "group", StringComparison.OrdinalIgnoreCase))
                continue;
            var label = spec.Label ?? "";
            if (needles.Any(n => label.Contains(n, StringComparison.OrdinalIgnoreCase)))
                return string.IsNullOrWhiteSpace(spec.Value) ? null : spec.Value;
        }
        return null;
    }
}
