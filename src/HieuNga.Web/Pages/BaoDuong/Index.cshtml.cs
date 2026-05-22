using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.BaoDuong;

public class IndexModel(IBookingService bookingService, IBranchService branchService) : PageModel
{
    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? MotorcycleModel { get; set; }
    [BindProperty] public string? LicensePlate { get; set; }
    [BindProperty] public string ServiceType { get; set; } = "Bảo dưỡng định kỳ";
    [BindProperty] public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(1);
    [BindProperty] public string? Notes { get; set; }
    public bool Success { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        this.SetSeo(null, "Đặt lịch bảo dưỡng | Honda Hiếu Nga HEAD",
            "Bảo dưỡng chính hãng Honda — kỹ thuật viên được đào tạo bài bản.");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(Phone))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập họ tên và số điện thoại.");
            return Page();
        }

        await bookingService.CreateMaintenanceBookingAsync(new CreateMaintenanceBookingDto(
            CustomerName, Phone, Email, MotorcycleModel, LicensePlate, ServiceType,
            PreferredDate, null, Notes, Branches.FirstOrDefault()?.Id), ct);
        Success = true;
        return Page();
    }
}
