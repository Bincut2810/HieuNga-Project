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
    public int MotorcycleCount { get; private set; }
    public int ServiceCount { get; private set; }
    public int BannerCount { get; private set; }
    public int BankCount { get; private set; }
    public IReadOnlyList<RecentRow> RecentMotorcycles { get; private set; } = [];

    public record RecentRow(Guid Id, string Name, DateTime? UpdatedAt);

    public async Task OnGetAsync(CancellationToken ct)
    {
        MotorcycleCount = (await motorcycles.GetAllAsync(ct)).Count;
        ServiceCount = (await services.GetAllAsync(ct)).Count;
        BannerCount = (await banners.GetAllAsync(ct)).Count;
        BankCount = (await banks.GetAllAsync(ct)).Count;

        RecentMotorcycles = await db.Motorcycles.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
            .Take(6)
            .Select(m => new RecentRow(m.Id, m.Name, m.UpdatedAt ?? m.CreatedAt))
            .ToListAsync(ct);
    }
}
