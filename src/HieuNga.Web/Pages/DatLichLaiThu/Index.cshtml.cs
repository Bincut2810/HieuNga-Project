using HieuNga.Application;
using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
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
    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    public MotorcycleDetailDto? SelectedMotorcycle { get; private set; }
    public string LeadSource { get; private set; } = "test-ride";

    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(1);
    [BindProperty] public string? PreferredTime { get; set; } = "09:00";
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public Guid? MotorcycleId { get; set; }
    [BindProperty] public Guid? BranchId { get; set; }
    [BindProperty] public string? SourceField { get; set; }

    public bool Success { get; private set; }
    public static readonly string[] TimeSlots = ["08:30", "09:00", "10:00", "11:00", "13:30", "14:30", "15:30", "16:30"];

    public async Task OnGetAsync([FromQuery] Guid? xeId, [FromQuery] string? source, CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        LeadSource = string.IsNullOrWhiteSpace(source) ? "test-ride" : source.Trim().ToLowerInvariant();
        MotorcycleId = xeId;
        BranchId = Branches.FirstOrDefault(b => b.IsHeadOffice)?.Id ?? Branches.FirstOrDefault()?.Id;
        if (xeId.HasValue)
        {
            // Resolve via related list / search — use GetBySlug if we only have id through service
            SelectedMotorcycle = await ResolveBikeByIdAsync(xeId.Value, ct);
        }
        this.SetSeo(null, "Đặt lịch xem xe | Xe Máy Hiếu Nga", "Đặt lịch xem / lái thử xe Honda tại showroom Hiếu Nga.");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        LeadSource = string.IsNullOrWhiteSpace(SourceField) ? "test-ride" : SourceField.Trim().ToLowerInvariant();
        if (MotorcycleId.HasValue)
            SelectedMotorcycle = await ResolveBikeByIdAsync(MotorcycleId.Value, ct);

        var attributedNotes = LeadAttribution.BuildNotes(
            LeadSource,
            "lai-thu",
            SelectedMotorcycle?.Slug,
            null,
            null,
            Notes,
            SelectedMotorcycle is null ? null : $"bike={SelectedMotorcycle.Name}");

        var dto = new CreateBookingDto(
            CustomerName, Phone, Email, PreferredDate, PreferredTime, attributedNotes, MotorcycleId, BranchId);
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return Page();
        }

        await bookingService.CreateTestRideBookingAsync(dto, ct);
        Success = true;
        this.SetSeo(null, "Đặt lịch thành công | Xe Máy Hiếu Nga", "Cảm ơn bạn đã đặt lịch xem xe.");
        return Page();
    }

    private async Task<MotorcycleDetailDto?> ResolveBikeByIdAsync(Guid id, CancellationToken ct)
    {
        var related = await motorcycleService.GetCompareListAsync([id], ct);
        var item = related.FirstOrDefault();
        if (item is null) return null;
        return await motorcycleService.GetBySlugAsync(item.Slug, ct);
    }
}
