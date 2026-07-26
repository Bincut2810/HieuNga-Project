using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Extensions;
using HieuNga.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Xe;

public class ChiTietModel(
    IMotorcycleService motorcycleService,
    IInstallmentService installmentService,
    IFinanceConfigService financeConfig,
    HieuNgaDbContext db) : PageModel
{
    public MotorcycleDetailDto? Motorcycle { get; private set; }
    public IReadOnlyList<MotorcycleListItemDto> Related { get; private set; } = [];
    public InstallmentCalculationDto? InitialFinance { get; private set; }
    public IReadOnlyList<FinanceBankDto> FinanceBanks { get; private set; } = [];
    public bool HasFinanceBanks => FinanceBanks.Count > 0;
    public bool ShowInstallmentCalculator { get; private set; } = true;
    public decimal DefaultDownPayment { get; private set; }
    public int DefaultTermMonths { get; private set; } = 12;
    public string? DefaultBankId { get; private set; }
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

        FinanceBanks = await financeConfig.GetActiveBanksAsync(ct);
        var prefs = await MotorcycleFinancePrefs.LoadAsync(db, Motorcycle.Id, ct);
        var hasPrice = Motorcycle.BasePrice > 0
                       || Motorcycle.Variants.Any(v => v.Price > 0);
        ShowInstallmentCalculator = prefs.CalculatorEnabled && hasPrice && FinanceBanks.Count > 0;

        Related = await motorcycleService.GetRelatedAsync(Motorcycle.Id, ct);

        var price = Motorcycle.Variants.FirstOrDefault(v => v.Price > 0)?.Price
                    ?? Motorcycle.Variants.FirstOrDefault()?.Price
                    ?? Motorcycle.BasePrice;
        var downPct = prefs.DefaultDownPaymentPercent > 0 ? prefs.DefaultDownPaymentPercent : 20m;
        DefaultTermMonths = prefs.DefaultTermMonths > 0 ? prefs.DefaultTermMonths : 12;
        DefaultDownPayment = Math.Round(price * (downPct / 100m) / 500_000m) * 500_000m;

        FinanceBankDto? defaultBank = null;
        if (!string.IsNullOrEmpty(prefs.DefaultBankId))
            defaultBank = FinanceBanks.FirstOrDefault(b => b.Id == prefs.DefaultBankId);
        defaultBank ??= await financeConfig.GetDefaultBankAsync(ct);
        DefaultBankId = defaultBank?.Id;

        if (defaultBank is not null && ShowInstallmentCalculator)
        {
            InitialFinance = installmentService.Calculate(
                price, DefaultDownPayment, DefaultTermMonths, defaultBank.MonthlyRate, bankName: defaultBank.Name);
        }

        ViewData["HideDefaultMobileCta"] = true;
        this.SetSeo(Motorcycle.Seo, $"{Motorcycle.Name} | Xe Máy Hiếu Nga", Motorcycle.ShortDescription);
        return Page();
    }

    public IActionResult OnGetCalculateFinancing(
        decimal vehiclePrice, decimal downPayment, int termMonths, decimal? monthlyRate, string? bankName)
    {
        var result = installmentService.Calculate(vehiclePrice, downPayment, termMonths, monthlyRate, bankName: bankName);
        return Partial("Shared/_DetailFinancingResult", result);
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
