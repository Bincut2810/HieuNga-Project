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

namespace HieuNga.Web.Pages.Admin.KhachHang;

public class BaoDuongIndexModel : PageModel
{
    public IActionResult OnGet() =>
        HieuNga.Web.Pages.Admin.Bookings.BookingCenterRedirect.ToCenter(Request, "maint");
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
