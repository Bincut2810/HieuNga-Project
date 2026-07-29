using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using FluentValidation;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.BaoDuong;

public class IndexModel(
    IBookingService bookingService,
    IServiceCatalogService serviceCatalog,
    IValidator<CreateMaintenanceBookingDto> validator) : PageModel
{
    public IReadOnlyList<string> BookingServiceOptions { get; private set; } = [];
    public IReadOnlyList<string> TimeSlots { get; } = Application.Validators.CreateMaintenanceBookingValidator.AllowedTimes;

    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string MotorcycleModel { get; set; } = "";
    [BindProperty] public string ServiceType { get; set; } = "";
    [BindProperty] public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(1);
    [BindProperty] public string PreferredTime { get; set; } = "09:00";
    [BindProperty] public string? Notes { get; set; }
    public bool Success { get; private set; }

    public async Task OnGetAsync(string? service, CancellationToken ct)
    {
        await LoadAsync(ct);

        if (!string.IsNullOrWhiteSpace(service))
        {
            var match = await serviceCatalog.GetBySlugAsync(service, ct);
            if (match is not null)
                ServiceType = match.Name;
        }

        this.SetSeo(null, "Đặt lịch bảo dưỡng | Xe Máy Hiếu Nga",
            "Đặt lịch bảo dưỡng Honda nhanh — xác nhận qua điện thoại.");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        var dto = new CreateMaintenanceBookingDto(
            CustomerName, Phone, MotorcycleModel, ServiceType,
            PreferredDate, PreferredTime, Notes);

        var result = await validator.ValidateAsync(dto, ct);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return Page();
        }

        await bookingService.CreateMaintenanceBookingAsync(dto, ct);
        Success = true;
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        BookingServiceOptions = await serviceCatalog.GetBookingServiceNamesAsync(ct);
        if (BookingServiceOptions.Count > 0 && !BookingServiceOptions.Contains(ServiceType))
            ServiceType = BookingServiceOptions[0];
    }
}
