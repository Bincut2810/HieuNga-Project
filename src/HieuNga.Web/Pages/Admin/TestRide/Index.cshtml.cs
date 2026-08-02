using HieuNga.Application.TestRide;
using HieuNga.Domain.Enums;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.TestRide;

public class IndexModel(ITestRideService bookingService, ILogger<IndexModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "today";

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public TestRideBoardResult Board { get; private set; } =
        new([], 0, 0, 0);

    public string ActiveRange =>
        Range is "tomorrow" or "all" ? Range : "today";

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Lịch xem xe";
        if (Range is not ("today" or "tomorrow" or "all"))
            Range = "today";
        Board = await bookingService.GetBoardAsync(Range, Q, ct);
    }

    public async Task<IActionResult> OnGetDetailAsync(Guid id, CancellationToken ct)
    {
        var item = await bookingService.GetByIdAsync(id, ct);
        if (item is null) return NotFound();
        return new JsonResult(item);
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        return await RunStatusAsync(id, BookingStatus.Confirmed, range, q, "Đã xác nhận lịch.", ct);
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        return await RunStatusAsync(id, BookingStatus.Completed, range, q, "Đã hoàn thành.", ct);
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        return await RunStatusAsync(id, BookingStatus.Cancelled, range, q, "Đã hủy lịch.", ct);
    }

    public async Task<IActionResult> OnPostSaveDetailAsync(
        Guid id, BookingStatus status, string? adminNotes, string range, string? q, CancellationToken ct)
    {
        try
        {
            await bookingService.UpdateAdminAsync(id, status, adminNotes, ct);
            this.SetSuccess("Đã lưu chi tiết lịch hẹn.");
        }
        catch (InvalidOperationException ex)
        {
            this.SetError(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TestRide admin save failed for {Id}", id);
            this.SetError("Không lưu được. Vui lòng thử lại.");
        }

        return RedirectToPage(new { range = range ?? "today", q });
    }

    private async Task<IActionResult> RunStatusAsync(
        Guid id, BookingStatus status, string range, string? q, string ok, CancellationToken ct)
    {
        try
        {
            await bookingService.UpdateStatusAsync(id, status, ct);
            this.SetSuccess(ok);
        }
        catch (InvalidOperationException ex)
        {
            this.SetError(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TestRide status update failed for {Id}", id);
            this.SetError("Không cập nhật được. Vui lòng thử lại.");
        }

        return RedirectToPage(new { range = range ?? "today", q });
    }
}
