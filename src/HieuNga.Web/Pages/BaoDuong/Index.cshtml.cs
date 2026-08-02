using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.TestRide;
using FluentValidation;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.BaoDuong;

public class IndexModel(
    IBookingService bookingService,
    IServiceCatalogService serviceCatalog,
    IValidator<CreateMaintenanceBookingDto> validator,
    ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<string> BookingServiceOptions { get; private set; } = [];
    public IReadOnlyList<string> TimeSlots { get; } = Application.Validators.CreateMaintenanceBookingValidator.AllowedTimes;

    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string MotorcycleModel { get; set; } = "";
    [BindProperty] public string ServiceType { get; set; } = "";
    [BindProperty] public DateTime PreferredDate { get; set; } = TestRideVietnamTime.Today.AddDays(1);
    [BindProperty] public string PreferredTime { get; set; } = TestRideValidator.AllowedAppointmentTimes[0];
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

    public Task<IActionResult> OnPostAsync(CancellationToken ct) =>
        ProcessAsync(jsonResponse: false, ct);

    public Task<IActionResult> OnPostBookAsync(CancellationToken ct) =>
        ProcessAsync(jsonResponse: true, ct);

    private async Task<IActionResult> ProcessAsync(bool jsonResponse, CancellationToken ct)
    {
        await LoadAsync(ct);

        var dto = new CreateMaintenanceBookingDto(
            CustomerName, Phone, MotorcycleModel, ServiceType,
            PreferredDate, PreferredTime, Notes);

        var result = await validator.ValidateAsync(dto, ct);
        if (!result.IsValid)
        {
            if (jsonResponse)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return new JsonResult(new
                {
                    success = false,
                    message = "Vui lòng kiểm tra lại thông tin.",
                    errors
                });
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return Page();
        }

        try
        {
            await bookingService.CreateMaintenanceBookingAsync(dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Maintenance booking failed for {Phone}", Phone);
            const string busy = "Hệ thống đang bận. Vui lòng thử lại sau hoặc gọi hotline.";
            if (jsonResponse)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = busy,
                    errors = new Dictionary<string, string[]> { [""] = [busy] }
                });
            }

            ModelState.AddModelError(string.Empty, busy);
            return Page();
        }

        if (jsonResponse)
            return new JsonResult(new { success = true, message = "Đặt lịch thành công" });

        Success = true;
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        BookingServiceOptions = await serviceCatalog.GetBookingServiceNamesAsync(ct);
        if (BookingServiceOptions.Count > 0 && !BookingServiceOptions.Contains(ServiceType))
            ServiceType = BookingServiceOptions[0];
        if (string.IsNullOrWhiteSpace(PreferredTime))
            PreferredTime = TimeSlots[0];
        if (PreferredDate == default)
            PreferredDate = TestRideVietnamTime.Today.AddDays(1);
    }
}
