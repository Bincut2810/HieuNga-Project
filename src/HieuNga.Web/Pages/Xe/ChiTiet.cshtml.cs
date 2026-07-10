using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Xe;

public class ChiTietModel(
    IMotorcycleService motorcycleService,
    IInstallmentService installmentService,
    IFinanceConfigService financeConfig) : PageModel
{
    public MotorcycleDetailDto? Motorcycle { get; private set; }
    public IReadOnlyList<MotorcycleListItemDto> Related { get; private set; } = [];
    public InstallmentCalculationDto? InitialFinance { get; private set; }
    public IReadOnlyList<FinanceBankDto> FinanceBanks { get; private set; } = [];
    public bool HasFinanceBanks => FinanceBanks.Count > 0;

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        Motorcycle = await motorcycleService.GetBySlugAsync(slug, ct);
        if (Motorcycle is null) return NotFound();

        FinanceBanks = await financeConfig.GetActiveBanksAsync(ct);

        var related = await motorcycleService.SearchAsync(
            new MotorcycleFilterDto(null, Motorcycle.Category, null, null, 1, 6), ct);
        Related = related.Items.Where(x => x.Id != Motorcycle.Id).Take(3).ToList();

        var price = Motorcycle.Variants.FirstOrDefault()?.Price ?? Motorcycle.BasePrice;
        var downPayment = Math.Round(price * 0.2m / 500_000m) * 500_000m;
        var defaultBank = await financeConfig.GetDefaultBankAsync(ct);
        if (defaultBank is not null)
        {
            InitialFinance = installmentService.Calculate(
                price, downPayment, 12, defaultBank.MonthlyRate, bankName: defaultBank.Name);
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
}
