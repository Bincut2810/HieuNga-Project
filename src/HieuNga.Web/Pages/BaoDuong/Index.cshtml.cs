using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.BaoDuong;

public class IndexModel(
    IBookingService bookingService,
    IBranchService branchService,
    IServiceCatalogService serviceCatalog) : PageModel
{
    public IReadOnlyList<ServiceItemListDto> Services { get; private set; } = [];
    public IReadOnlyList<string> BookingServiceOptions { get; private set; } = [];
    public string PricingDisclaimer { get; private set; } = "";
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

    public async Task OnGetAsync(string? service, CancellationToken ct)
    {
        await LoadCatalogAsync(ct);
        Branches = await branchService.GetActiveAsync(ct);

        if (!string.IsNullOrWhiteSpace(service))
        {
            var match = await serviceCatalog.GetBySlugAsync(service, ct);
            if (match is not null)
                ServiceType = match.Name;
        }

        this.SetSeo(null, "Đặt lịch bảo dưỡng | Honda Hiếu Nga HEAD",
            "Bảo dưỡng chính hãng Honda — kỹ thuật viên được đào tạo bài bản.");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadCatalogAsync(ct);
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

    private async Task LoadCatalogAsync(CancellationToken ct)
    {
        Services = await serviceCatalog.GetActiveItemsAsync(ct);
        BookingServiceOptions = await serviceCatalog.GetBookingServiceNamesAsync(ct);
        PricingDisclaimer = serviceCatalog.PricingDisclaimer;
        if (BookingServiceOptions.Count > 0 && !BookingServiceOptions.Contains(ServiceType))
            ServiceType = BookingServiceOptions[0];
    }
}
