using HieuNga.Application;
using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.TraGop;

public class IndexModel(IInstallmentService installmentService, IMotorcycleService motorcycleService) : PageModel
{
    public InstallmentCalculationDto? Result { get; private set; }
    public MotorcycleDetailDto? SelectedMotorcycle { get; private set; }
    public string LeadSource { get; private set; } = "finance";
    public bool InquirySuccess { get; private set; }

    [BindProperty] public decimal VehiclePrice { get; set; } = 50_000_000;
    [BindProperty] public decimal DownPayment { get; set; } = 10_000_000;
    [BindProperty] public int TermMonths { get; set; } = 12;

    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? InquiryNotes { get; set; }
    [BindProperty] public Guid? MotorcycleId { get; set; }
    [BindProperty] public string? SourceField { get; set; }
    [BindProperty] public string? XeSlugField { get; set; }

    public async Task OnGetAsync(string? xe, string? source, CancellationToken ct)
    {
        LeadSource = string.IsNullOrWhiteSpace(source) ? "finance" : source.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(xe))
        {
            SelectedMotorcycle = await motorcycleService.GetBySlugAsync(xe.Trim(), ct);
            if (SelectedMotorcycle is not null)
            {
                MotorcycleId = SelectedMotorcycle.Id;
                VehiclePrice = SelectedMotorcycle.Variants.FirstOrDefault()?.Price ?? SelectedMotorcycle.BasePrice;
                DownPayment = Math.Round(VehiclePrice * 0.2m / 500_000m) * 500_000m;
                Result = installmentService.Calculate(VehiclePrice, DownPayment, TermMonths);
            }
        }

        this.SetSeo(null, "Tính trả góp xe máy Honda | Xe Máy Hiếu Nga",
            "Công cụ tính trả góp xe máy Honda nhanh chóng, minh bạch.");
    }

    public IActionResult OnPostCalculate()
    {
        Result = installmentService.Calculate(VehiclePrice, DownPayment, TermMonths);
        return Partial("_CalculatorResult", Result);
    }

    public async Task<IActionResult> OnPostInquiryAsync(CancellationToken ct)
    {
        LeadSource = string.IsNullOrWhiteSpace(SourceField) ? "finance" : SourceField.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(XeSlugField))
            SelectedMotorcycle = await motorcycleService.GetBySlugAsync(XeSlugField, ct);
        MotorcycleId ??= SelectedMotorcycle?.Id;
        if (SelectedMotorcycle is not null && VehiclePrice <= 0)
            VehiclePrice = SelectedMotorcycle.BasePrice;

        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(Phone))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập họ tên và số điện thoại.");
            Result = installmentService.Calculate(VehiclePrice, DownPayment, TermMonths);
            return Page();
        }

        if (DownPayment >= VehiclePrice)
            DownPayment = Math.Round(VehiclePrice * 0.2m / 500_000m) * 500_000m;

        var notes = LeadAttribution.BuildNotes(
            LeadSource,
            "tra-gop",
            SelectedMotorcycle?.Slug ?? XeSlugField,
            null,
            "Yêu cầu tư vấn trả góp",
            InquiryNotes,
            $"price={VehiclePrice};down={DownPayment};term={TermMonths}");

        await installmentService.SubmitRequestAsync(
            new CreateInstallmentRequestDto(
                CustomerName, Phone, Email, MotorcycleId, VehiclePrice, DownPayment, TermMonths, notes),
            ct);

        InquirySuccess = true;
        Result = installmentService.Calculate(VehiclePrice, DownPayment, TermMonths);
        this.SetSeo(null, "Đã gửi yêu cầu trả góp | Xe Máy Hiếu Nga", "Cảm ơn bạn đã gửi yêu cầu trả góp.");
        return Page();
    }
}
