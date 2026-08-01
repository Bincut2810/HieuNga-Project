using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HieuNga.Web.Pages.Admin.KhachHang;

public class BaoDuongIndexModel(IBookingService bookingService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "today";

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public MaintenanceBoardDto Board { get; private set; } =
        new([], new MaintenanceBoardCounts(0, 0, 0, 0));

    public IReadOnlyList<MaintenanceBookingDto> NewItems =>
        Board.Items.Where(b => b.Status == BookingStatus.Pending).ToList();
    public IReadOnlyList<MaintenanceBookingDto> ConfirmedItems =>
        Board.Items.Where(b => b.Status == BookingStatus.Confirmed).ToList();
    public IReadOnlyList<MaintenanceBookingDto> CompletedItems =>
        Board.Items.Where(b => b.Status == BookingStatus.Completed).ToList();
    public IReadOnlyList<MaintenanceBookingDto> CancelledItems =>
        Board.Items.Where(b => b.Status == BookingStatus.Cancelled).ToList();

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Lịch bảo dưỡng";
        Board = await bookingService.GetMaintenanceBoardAsync(Range, Q, ct);
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        await bookingService.UpdateMaintenanceStatusAsync(id, BookingStatus.Confirmed, ct);
        this.SetSuccess("Đã xác nhận lịch hẹn.");
        return RedirectToPage(new { range, q });
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        await bookingService.UpdateMaintenanceStatusAsync(id, BookingStatus.Completed, ct);
        this.SetSuccess("Đã hoàn thành.");
        return RedirectToPage(new { range, q });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        await bookingService.UpdateMaintenanceStatusAsync(id, BookingStatus.Cancelled, ct);
        this.SetSuccess("Đã hủy lịch hẹn.");
        return RedirectToPage(new { range, q });
    }
}

public class LichHenIndexModel(IBookingService bookingService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "today";

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public TestRideBoardDto Board { get; private set; } =
        new([], new TestRideBoardCounts(0, 0, 0, 0, 0, 0));

    public IReadOnlyList<TestRideBookingDto> WaitingItems =>
        Board.Items.Where(b => b.Status == BookingStatus.Pending).ToList();
    public IReadOnlyList<TestRideBookingDto> ConfirmedItems =>
        Board.Items.Where(b => b.Status == BookingStatus.Confirmed).ToList();
    public IReadOnlyList<TestRideBookingDto> CompletedItems =>
        Board.Items.Where(b => b.Status == BookingStatus.Completed).ToList();
    public IReadOnlyList<TestRideBookingDto> CancelledItems =>
        Board.Items.Where(b => b.Status == BookingStatus.Cancelled).ToList();

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Lịch xem xe";
        await LoadBoardAsync(ct);
    }

    public async Task<IActionResult> OnGetRefreshAsync(CancellationToken ct)
    {
        await LoadBoardAsync(ct);
        return Partial("Admin/KhachHang/LichHen/_BoardRefresh", this);
    }

    private async Task LoadBoardAsync(CancellationToken ct)
    {
        Board = await bookingService.GetTestRideBoardAsync("all", Q, ct);
        if (string.IsNullOrWhiteSpace(Range)
            || Range is not ("today" or "tomorrow" or "all"))
            Range = "today";
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        await bookingService.UpdateTestRideStatusAsync(id, BookingStatus.Confirmed, ct);
        this.SetSuccess("Đã xác nhận lịch xem xe.");
        return RedirectToPage(new { range = range ?? "today", q });
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        await bookingService.UpdateTestRideStatusAsync(id, BookingStatus.Completed, ct);
        this.SetSuccess("Đã hoàn thành.");
        return RedirectToPage(new { range = range ?? "today", q });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, string range, string? q, CancellationToken ct)
    {
        await bookingService.UpdateTestRideStatusAsync(id, BookingStatus.Cancelled, ct);
        this.SetSuccess("Đã hủy lịch xem xe.");
        return RedirectToPage(new { range = range ?? "today", q });
    }
}

public class LichHenDetailModel(IBookingService bookingService, HieuNgaDbContext db) : PageModel
{
    public Booking? Booking { get; private set; }

    [BindProperty]
    public DetailInput Input { get; set; } = new();

    public class DetailInput
    {
        [Required] public BookingStatus Status { get; set; }
        public string? AdminNotes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Chi tiết lịch xem xe";
        Booking = await db.Bookings.AsNoTracking()
            .Include(b => b.Motorcycle).Include(b => b.Branch)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted && b.Type == BookingType.TestRide, ct);
        if (Booking is null) return NotFound();
        Input = new DetailInput { Status = Booking.Status, AdminNotes = Booking.AdminNotes };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await bookingService.UpdateTestRideAdminAsync(id, Input.Status, Input.AdminNotes, ct);
            this.SetSuccess("Đã cập nhật lịch xem xe.");
        }
        catch (InvalidOperationException ex)
        {
            this.SetError(ex.Message);
        }

        return RedirectToPage(new { id });
    }
}

public class TraGopLeadIndexModel(HieuNgaDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    public IReadOnlyList<Row> Items { get; private set; } = [];
    public record Row(Guid Id, string CustomerName, string Phone, decimal VehiclePrice, bool IsProcessed, DateTime CreatedAt);

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Yêu cầu trả góp";
        var q = db.InstallmentRequests.AsNoTracking().Where(r => !r.IsDeleted);
        if (!string.IsNullOrWhiteSpace(Q))
            q = q.Where(r => r.CustomerName.Contains(Q) || r.Phone.Contains(Q));
        Items = await q.OrderByDescending(r => r.CreatedAt)
            .Select(r => new Row(r.Id, r.CustomerName, r.Phone, r.VehiclePrice, r.IsProcessed, r.CreatedAt))
            .ToListAsync(ct);
    }
}

public class TraGopLeadDetailModel(IRepository<InstallmentRequest> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    public InstallmentRequest? Lead { get; private set; }

    [BindProperty]
    public DetailInput Input { get; set; } = new();

    public class DetailInput
    {
        public bool IsProcessed { get; set; }
        public string? AdminNotes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Chi tiết trả góp";
        Lead = await db.InstallmentRequests.AsNoTracking()
            .Include(r => r.Motorcycle)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
        if (Lead is null) return NotFound();
        Input = new DetailInput { IsProcessed = Lead.IsProcessed, AdminNotes = Lead.AdminNotes };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();
        entity.IsProcessed = Input.IsProcessed;
        entity.AdminNotes = Input.AdminNotes;
        await repo.UpdateAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật yêu cầu.");
        return RedirectToPage(new { id });
    }
}
