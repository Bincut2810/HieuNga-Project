using HieuNga.Application.TestRide;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin;

public class IndexModel(
    IMotorcycleRepository motorcycles,
    IBannerRepository banners,
    IRepository<ServiceItem> services,
    IRepository<Bank> banks,
    HieuNgaDbContext db) : PageModel
{
    // ── Shared ──────────────────────────────────────────────
    public int MotorcycleCount { get; private set; }
    public int ServiceCount { get; private set; }
    public int BannerCount { get; private set; }
    public int BankCount { get; private set; }
    public int BankNoRateCount { get; private set; }
    public IReadOnlyList<RecentRow> RecentMotorcycles { get; private set; } = [];

    // ── Content staff ───────────────────────────────────────
    public int DraftNewsCount { get; private set; }
    public int PublishedNewsCount { get; private set; }
    public int ActivePromotionCount { get; private set; }
    public int ActiveBannerCount { get; private set; }

    // ── Booking staff ───────────────────────────────────────
    public int NewBookingCount { get; private set; }
    public int TodayBookingCount { get; private set; }

    public record RecentRow(Guid Id, string Name, DateTime? UpdatedAt);

    public async Task OnGetAsync(CancellationToken ct)
    {
        // ── Always available ─────────────────────────────────
        MotorcycleCount = (await motorcycles.GetAllAsync(ct)).Count;
        ServiceCount = (await services.GetAllAsync(ct)).Count;
        BannerCount = (await banners.GetAllAsync(ct)).Count;
        BankCount = (await banks.GetAllAsync(ct)).Count;

        // Use the same group-by pattern as Admin/TraGop NganHang/Index —
        // proven to translate on PostgreSQL via Npgsql.
        var rateCounts = await db.FinanceRates.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .GroupBy(r => r.BankId)
            .Select(g => new { BankId = g.Key, Active = g.Count(r => r.IsActive) })
            .ToListAsync(ct);
        var activeBankIdsWithRates = rateCounts.Where(r => r.Active > 0).Select(r => r.BankId).ToHashSet();
        BankNoRateCount = await db.Banks.AsNoTracking()
            .Where(b => !b.IsDeleted && b.IsActive)
            .Where(b => !activeBankIdsWithRates.Contains(b.Id))
            .CountAsync(ct);

        RecentMotorcycles = await db.Motorcycles.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
            .Take(6)
            .Select(m => new RecentRow(m.Id, m.Name, m.UpdatedAt ?? m.CreatedAt))
            .ToListAsync(ct);

        // ── Content staff ────────────────────────────────────
        DraftNewsCount = await db.BlogPosts.AsNoTracking().Where(p => !p.IsDeleted && !p.IsPublished).CountAsync(ct);
        PublishedNewsCount = await db.BlogPosts.AsNoTracking().Where(p => p.IsPublished && !p.IsDeleted).CountAsync(ct);
        var now = DateTime.UtcNow;
        ActivePromotionCount = await db.Promotions.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive && p.StartDate <= now && p.EndDate >= now).CountAsync(ct);
        ActiveBannerCount = await db.Banners.AsNoTracking().Where(b => !b.IsDeleted && b.IsActive).CountAsync(ct);

        // ── Booking staff ───────────────────────────────────
        // Use UTC day-range pattern (same as BookingService / TestRideService).
        // .Date on DateTime cannot be translated to SQL on Npgsql.
        var todayVn = TestRideVietnamTime.Today;
        var todayStartUtc = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn);
        var tomorrowStartUtc = TestRideVietnamTime.ConvertLocalAppointmentDateEndExclusiveToUtc(todayVn);

        NewBookingCount = await db.Bookings.AsNoTracking()
            .Where(b => b.Status == Domain.Enums.BookingStatus.Pending)
            .CountAsync(ct);
        TodayBookingCount = await db.Bookings.AsNoTracking()
            .Where(b => b.Status != Domain.Enums.BookingStatus.Cancelled
                && b.PreferredDate >= todayStartUtc
                && b.PreferredDate < tomorrowStartUtc)
            .CountAsync(ct);
    }
}
