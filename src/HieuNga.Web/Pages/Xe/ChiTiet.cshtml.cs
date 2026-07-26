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
    public decimal EffectivePrice { get; private set; }
    public decimal DefaultDownPayment { get; private set; }
    public int DefaultTermMonths { get; private set; } = 12;
    public string? DefaultBankId { get; private set; }
    public string DefaultBankName { get; private set; } = MotorcycleFinancePrefs.FallbackBankName;
    public decimal DefaultMonthlyRate { get; private set; } = MotorcycleFinancePrefs.FallbackMonthlyRate;
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

        // Self-heal: every published detail view gets finance prefs if missing
        await MotorcycleFinancePrefs.EnsureDefaultsAsync(db, Motorcycle.Id, ct);
        var prefs = await MotorcycleFinancePrefs.LoadAsync(db, Motorcycle.Id, ct);

        EffectivePrice = MotorcycleFinancePrefs.ResolveEffectivePrice(
            Motorcycle.BasePrice,
            Motorcycle.Variants.Select(v => v.Price));

        FinanceBanks = await financeConfig.GetActiveBanksAsync(ct);

        // Show finance whenever the bike has a selling price and calculator is enabled.
        // Banks are optional — JS + FallbackMonthlyRate keep the calculator usable.
        ShowInstallmentCalculator = prefs.CalculatorEnabled && EffectivePrice > 0;

        Related = await motorcycleService.GetRelatedAsync(Motorcycle.Id, ct);

        var downPct = prefs.DefaultDownPaymentPercent;
        DefaultTermMonths = prefs.DefaultTermMonths;
        DefaultDownPayment = EffectivePrice > 0
            ? Math.Round(EffectivePrice * (downPct / 100m) / 500_000m) * 500_000m
            : 0m;

        FinanceBankDto? defaultBank = null;
        if (!string.IsNullOrEmpty(prefs.DefaultBankId))
            defaultBank = FinanceBanks.FirstOrDefault(b => b.Id == prefs.DefaultBankId);
        defaultBank ??= await financeConfig.GetDefaultBankAsync(ct);

        DefaultBankId = defaultBank?.Id;
        DefaultBankName = defaultBank?.Name ?? MotorcycleFinancePrefs.FallbackBankName;
        DefaultMonthlyRate = defaultBank?.MonthlyRate ?? MotorcycleFinancePrefs.FallbackMonthlyRate;

        if (ShowInstallmentCalculator)
        {
            InitialFinance = installmentService.Calculate(
                EffectivePrice,
                DefaultDownPayment,
                DefaultTermMonths,
                DefaultMonthlyRate,
                bankName: DefaultBankName);
        }

        ViewData["HideDefaultMobileCta"] = true;
        this.SetSeo(Motorcycle.Seo, $"{Motorcycle.Name} | Xe Máy Hiếu Nga", Motorcycle.ShortDescription);
        return Page();
    }

    public IActionResult OnGetCalculateFinancing(
        decimal vehiclePrice, decimal downPayment, int termMonths, decimal? monthlyRate, string? bankName)
    {
        var result = installmentService.Calculate(
            vehiclePrice,
            downPayment,
            termMonths,
            monthlyRate ?? MotorcycleFinancePrefs.FallbackMonthlyRate,
            bankName: bankName ?? MotorcycleFinancePrefs.FallbackBankName);
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
