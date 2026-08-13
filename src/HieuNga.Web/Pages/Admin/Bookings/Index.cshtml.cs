using HieuNga.Application.Bookings;
using HieuNga.Application.Interfaces;
using HieuNga.Application.TestRide;
using HieuNga.Domain.Enums;
using HieuNga.Web.Pages.Admin.Extensions;
using HieuNga.Web.ViewModels.Admin.BookingCenter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Bookings;

public class IndexModel(
    ITestRideService testRideService,
    IBookingService bookingService,
    ILogger<IndexModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Type { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "today";

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int? PageNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    public string ActiveType => Type is "testride" or "maint" ? Type : "all";
    public string ActiveRange => BookingQuery.NormalizeRange(Range);

    public BookingCenterSummaryVm Summary { get; private set; } = new();
    public IReadOnlyList<BookingCenterTimelineGroupVm> Timeline { get; private set; } = [];
    public int VisibleCount { get; private set; }
    public string EmptyMessage { get; private set; } = "Không có lịch hẹn.";

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Booking Center";
        Type = ActiveType;
        Range = ActiveRange;
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnGetDetailAsync(Guid id, string kind, CancellationToken ct)
    {
        var nowVn = TestRideVietnamTime.Now;
        BookingDetailViewModel? detail = null;

        if (string.Equals(kind, "testride", StringComparison.OrdinalIgnoreCase))
        {
            var item = await testRideService.GetByIdAsync(id, ct);
            if (item is not null)
                detail = BookingDetailViewModel.FromTestRide(item, nowVn);
        }
        else if (string.Equals(kind, "maint", StringComparison.OrdinalIgnoreCase))
        {
            var item = await bookingService.GetMaintenanceByIdAsync(id, ct);
            if (item is not null)
                detail = BookingDetailViewModel.FromMaintenance(item, nowVn);
        }

        if (detail is null) return NotFound();
        return new JsonResult(detail);
    }

    public Task<IActionResult> OnPostConfirmAsync(
        Guid id, string kind, string type, string range, string? search, CancellationToken ct) =>
        RunStatusAsync(id, kind, BookingStatus.Confirmed, type, range, search, "Đã đánh dấu khách đến.", ct);

    public Task<IActionResult> OnPostCompleteAsync(
        Guid id, string kind, string type, string range, string? search, CancellationToken ct) =>
        RunStatusAsync(id, kind, BookingStatus.Completed, type, range, search, "Đã hoàn thành.", ct);

    public Task<IActionResult> OnPostCancelAsync(
        Guid id, string kind, string type, string range, string? search, CancellationToken ct) =>
        RunStatusAsync(id, kind, BookingStatus.Cancelled, type, range, search, "Đã hủy lịch.", ct);

    public async Task<IActionResult> OnPostSaveDetailAsync(
        Guid id, BookingStatus status, string? adminNotes,
        string type, string range, string? search, CancellationToken ct)
    {
        try
        {
            await testRideService.UpdateAdminAsync(id, status, adminNotes, ct);
            this.SetSuccess("Đã lưu chi tiết lịch hẹn.");
        }
        catch (InvalidOperationException ex)
        {
            this.SetError(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Booking Center save failed for {Id}", id);
            this.SetError("Không lưu được. Vui lòng thử lại.");
        }

        return RedirectToPage(BoardRoute(type, range, search));
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var nowVn = TestRideVietnamTime.Now;
        var includeTr = ActiveType is "all" or "testride";
        var includeMaint = ActiveType is "all" or "maint";
        var boardQuery = BookingQuery.FromAdmin(ActiveRange, Search, page: PageNumber, pageSize: PageSize);

        var loaded = await FetchBoardAsync(includeTr, includeMaint, boardQuery, nowVn, ct);

        // Late: SQL prefilters open appointments through today; time-of-day lateness is presentation-only.
        if (ActiveRange == "late")
            loaded = loaded.Where(i => i.IsLate).ToList();

        // Summary: reuse board rows when already on today without search; otherwise one today query (no search).
        IReadOnlyList<BookingCenterItemVm> todayItems;
        if (ActiveRange == "today" && string.IsNullOrWhiteSpace(Search))
        {
            todayItems = loaded;
        }
        else
        {
            todayItems = await FetchBoardAsync(
                includeTr,
                includeMaint,
                BookingQuery.FromAdmin("today", search: null),
                nowVn,
                ct);
        }

        Summary = BookingCenterMapper.BuildSummary(todayItems);
        VisibleCount = loaded.Count;
        Timeline = BookingCenterMapper.GroupByTime(loaded);
        EmptyMessage = BuildEmptyMessage();
    }

    private async Task<List<BookingCenterItemVm>> FetchBoardAsync(
        bool includeTr,
        bool includeMaint,
        BookingQuery query,
        DateTime nowVn,
        CancellationToken ct)
    {
        var list = new List<BookingCenterItemVm>();
        var trQuery = WithType(query, BookingType.TestRide);
        var maintQuery = WithType(query, bookingType: null);

        if (includeTr && includeMaint)
        {
            var trTask = testRideService.GetBoardAsync(trQuery, ct);
            var mTask = bookingService.GetMaintenanceBoardAsync(maintQuery, ct);
            await Task.WhenAll(trTask, mTask);
            list.AddRange((await trTask).Items.Select(b => BookingCenterMapper.FromTestRide(b, nowVn)));
            list.AddRange((await mTask).Items.Select(b => BookingCenterMapper.FromMaintenance(b, nowVn)));
        }
        else if (includeTr)
        {
            var tr = await testRideService.GetBoardAsync(trQuery, ct);
            list.AddRange(tr.Items.Select(b => BookingCenterMapper.FromTestRide(b, nowVn)));
        }
        else if (includeMaint)
        {
            var m = await bookingService.GetMaintenanceBoardAsync(maintQuery, ct);
            list.AddRange(m.Items.Select(b => BookingCenterMapper.FromMaintenance(b, nowVn)));
        }

        return list;
    }

    private static BookingQuery WithType(BookingQuery query, BookingType? bookingType) => new()
    {
        BookingType = bookingType,
        DateRange = query.DateRange,
        Status = query.Status,
        Search = query.Search,
        Page = query.Page,
        PageSize = query.PageSize
    };

    private string BuildEmptyMessage()
    {
        var typePart = ActiveType switch
        {
            "testride" => "xem xe",
            "maint" => "bảo dưỡng",
            _ => "hẹn"
        };
        return ActiveRange switch
        {
            "today" => $"Không có lịch {typePart} hôm nay",
            "tomorrow" => $"Không có lịch {typePart} ngày mai",
            "week" => $"Không có lịch {typePart} trong tuần này",
            "late" => "Không có lịch trễ giờ",
            "completed" => "Không có lịch đã hoàn thành",
            "cancelled" => "Không có lịch đã hủy",
            _ => $"Không có lịch {typePart}"
        };
    }

    private async Task<IActionResult> RunStatusAsync(
        Guid id, string kind, BookingStatus status,
        string type, string range, string? search, string ok, CancellationToken ct)
    {
        try
        {
            if (string.Equals(kind, "maint", StringComparison.OrdinalIgnoreCase))
                await bookingService.UpdateMaintenanceStatusAsync(id, status, ct);
            else
                await testRideService.UpdateStatusAsync(id, status, ct);
            this.SetSuccess(ok);
        }
        catch (InvalidOperationException ex)
        {
            this.SetError(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Booking Center status update failed for {Id}", id);
            this.SetError("Không cập nhật được. Vui lòng thử lại.");
        }

        return RedirectToPage(BoardRoute(type, range, search));
    }

    private static object BoardRoute(string? type, string? range, string? search) => new
    {
        type = type is "testride" or "maint" ? type : "all",
        range = BookingQuery.NormalizeRange(range),
        search
    };
}
