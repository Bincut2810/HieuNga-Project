using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.LienHe;

public class IndexModel(IBranchService branchService, IBookingService bookingService) : PageModel
{
    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Subject { get; set; }
    [BindProperty] public string? Message { get; set; }
    public bool Success { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        this.SetSeo(null, "Liên hệ Honda Hiếu Nga Đà Nẵng", "Hotline, Zalo, bản đồ showroom và form tư vấn miễn phí.");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(Phone))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập họ tên và số điện thoại.");
            return Page();
        }

        await bookingService.CreateConsultationAsync(
            new CreateConsultationDto(CustomerName, Phone, Email, Subject, Message, Branches.FirstOrDefault()?.Id), ct);
        Success = true;
        return Page();
    }
}
