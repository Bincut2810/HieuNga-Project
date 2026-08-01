using HieuNga.Application;
using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Validators;
using HieuNga.Web.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.DatLichLaiThu;

public class IndexModel(
    IBookingService bookingService,
    IBranchService branchService,
    IMotorcycleService motorcycleService,
    IValidator<CreateBookingDto> validator) : PageModel
{
    public IReadOnlyList<MotorcycleListItemDto> MotorcycleOptions { get; private set; } = [];
    public MotorcycleListItemDto? SelectedMotorcycle { get; private set; }
    public string LeadSource { get; private set; } = "test-ride";
    public IReadOnlyList<string> TimeSlots { get; } = CreateBookingValidator.AllowedTimeSlots;

    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public DateTime PreferredDate { get; set; } = DateTime.Today;
    [BindProperty] public string PreferredTime { get; set; } = CreateBookingValidator.AllowedTimeSlots[0];
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public Guid? MotorcycleId { get; set; }
    [BindProperty] public string? SourceField { get; set; }

    public async Task OnGetAsync([FromQuery] Guid? xeId, [FromQuery] string? source, CancellationToken ct)
    {
        await LoadAsync(xeId, source, ct);
        this.SetSeo(null, "Đặt lịch xem xe | Xe Máy Hiếu Nga",
            "Đặt lịch xem / lái thử xe Honda tại showroom Hiếu Nga — chỉ mất khoảng 30 giây.");
    }

    public async Task<IActionResult> OnPostBookAsync(CancellationToken ct)
    {
        LeadSource = string.IsNullOrWhiteSpace(SourceField) ? "test-ride" : SourceField.Trim().ToLowerInvariant();
        SelectedMotorcycle = MotorcycleId.HasValue
            ? await motorcycleService.GetListItemByIdAsync(MotorcycleId.Value, ct)
            : null;

        var branches = await branchService.GetActiveAsync(ct);
        var branchId = branches.FirstOrDefault(b => b.IsHeadOffice)?.Id ?? branches.FirstOrDefault()?.Id;

        var attributedNotes = LeadAttribution.BuildNotes(
            LeadSource,
            "lai-thu",
            SelectedMotorcycle?.Slug,
            null,
            null,
            Notes,
            SelectedMotorcycle is null ? null : $"bike={SelectedMotorcycle.Name}");

        var dto = new CreateBookingDto(
            CustomerName,
            Phone,
            Email: null,
            PreferredDate,
            PreferredTime,
            attributedNotes,
            MotorcycleId,
            branchId);

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        await bookingService.CreateTestRideBookingAsync(dto, ct);

        return new JsonResult(new
        {
            success = true,
            message = "Đã gửi lịch xem xe thành công.",
            motorcycleUrl = SelectedMotorcycle is null ? "/xe" : $"/xe/{SelectedMotorcycle.Slug}"
        });
    }

    private async Task LoadAsync(Guid? xeId, string? source, CancellationToken ct)
    {
        MotorcycleOptions = await motorcycleService.GetPublishedOptionsAsync(ct);
        LeadSource = string.IsNullOrWhiteSpace(source) ? "test-ride" : source.Trim().ToLowerInvariant();
        MotorcycleId = xeId;
        if (xeId.HasValue)
            SelectedMotorcycle = await motorcycleService.GetListItemByIdAsync(xeId.Value, ct);
    }
}
