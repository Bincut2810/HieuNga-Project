using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.TraGop;

public class IndexModel(IInstallmentService installmentService) : PageModel
{
    public InstallmentCalculationDto? Result { get; private set; }
    [BindProperty] public decimal VehiclePrice { get; set; } = 50_000_000;
    [BindProperty] public decimal DownPayment { get; set; } = 10_000_000;
    [BindProperty] public int TermMonths { get; set; } = 12;

    public void OnGet()
    {
        this.SetSeo(null, "Tính trả góp xe máy Honda | Xe Máy Hiếu Nga",
            "Công cụ tính trả góp xe máy Honda nhanh chóng, minh bạch.");
    }

    public IActionResult OnPostCalculate()
    {
        Result = installmentService.Calculate(VehiclePrice, DownPayment, TermMonths);
        return Partial("_CalculatorResult", Result);
    }
}
