using FluentValidation;
using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.DatLichLaiThu;

public class IndexModel(IBookingService bookingService, IValidator<CreateBookingDto> validator) : PageModel
{
    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(1);
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public Guid? MotorcycleId { get; set; }
    public bool Success { get; private set; }

    public void OnGet([FromQuery] Guid? xeId)
    {
        MotorcycleId = xeId;
        this.SetSeo(null, "Đặt lịch lái thử | Xe Máy Hiếu Nga", "Đặt lịch lái thử xe Honda miễn phí tại Đà Nẵng.");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var dto = new CreateBookingDto(CustomerName, Phone, Email, PreferredDate, null, Notes, MotorcycleId, null);
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return Page();
        }

        await bookingService.CreateTestRideBookingAsync(dto, ct);
        Success = true;
        return Page();
    }
}
