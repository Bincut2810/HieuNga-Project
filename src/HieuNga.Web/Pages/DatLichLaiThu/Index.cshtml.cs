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
    IValidator<CreateBookingDto> validator,
    ILogger<IndexModel> logger) : PageModel
{
    public const string DuplicateMessage =
        "Bạn đã gửi lịch hẹn trước đó. Nhân viên sẽ sớm liên hệ với bạn.";

    public IReadOnlyList<MotorcycleListItemDto> MotorcycleOptions { get; private set; } = [];
    public MotorcycleListItemDto? SelectedMotorcycle { get; private set; }
    public string LeadSource { get; private set; } = "test-ride";
    public IReadOnlyList<string> TimeSlots { get; } = CreateBookingValidator.AllowedTimeSlots;
    public bool Success { get; private set; }
    public bool IsDuplicate { get; private set; }
    public string SuccessMotorcycleUrl { get; private set; } = "/xe";

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

    /// <summary>Native POST fallback (no JS) — same booking path as AJAX.</summary>
    public Task<IActionResult> OnPostAsync(CancellationToken ct) =>
        ProcessBookingAsync(jsonResponse: false, ct);

    /// <summary>AJAX booking endpoint.</summary>
    public Task<IActionResult> OnPostBookAsync(CancellationToken ct) =>
        ProcessBookingAsync(jsonResponse: true, ct);

    private async Task<IActionResult> ProcessBookingAsync(bool jsonResponse, CancellationToken ct)
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

        var motorcycleUrl = SelectedMotorcycle is null ? "/xe" : $"/xe/{SelectedMotorcycle.Slug}";

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            if (jsonResponse)
            {
                return new JsonResult(new
                {
                    success = false,
                    errors = validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                });
            }

            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            await LoadAsync(MotorcycleId, LeadSource, ct);
            this.SetSeo(null, "Đặt lịch xem xe | Xe Máy Hiếu Nga",
                "Đặt lịch xem / lái thử xe Honda tại showroom Hiếu Nga — chỉ mất khoảng 30 giây.");
            return Page();
        }

        CreateTestRideResult result;
        try
        {
            result = await bookingService.CreateTestRideBookingAsync(dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test-ride booking failed for phone {Phone}", Phone);
            if (jsonResponse)
            {
                return new JsonResult(new
                {
                    success = false,
                    errors = new Dictionary<string, string[]>
                    {
                        [""] = ["Hệ thống đang bận. Vui lòng thử lại sau hoặc gọi hotline."]
                    }
                });
            }

            ModelState.AddModelError(string.Empty, "Hệ thống đang bận. Vui lòng thử lại sau hoặc gọi hotline.");
            await LoadAsync(MotorcycleId, LeadSource, ct);
            return Page();
        }

        var message = result.IsDuplicate
            ? DuplicateMessage
            : "Đặt lịch thành công!";

        if (jsonResponse)
        {
            return new JsonResult(new
            {
                success = true,
                duplicate = result.IsDuplicate,
                message,
                motorcycleUrl
            });
        }

        Success = true;
        IsDuplicate = result.IsDuplicate;
        SuccessMotorcycleUrl = motorcycleUrl;
        this.SetSeo(null, "Đặt lịch thành công | Xe Máy Hiếu Nga",
            "Cảm ơn bạn đã đăng ký lịch xem xe tại Xe Máy Hiếu Nga.");
        return Page();
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
