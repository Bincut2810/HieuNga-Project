using HieuNga.Application.Mappings;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.KhuyenMai;

public class IndexModel(HieuNgaDbContext db) : PageModel
{
    public IReadOnlyList<Application.DTOs.PromotionDto> Items { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Khuyến mãi";
        var query = db.Promotions.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(p => p.Title.Contains(Search) || (p.Slug != null && p.Slug.Contains(Search)));

        var all = await query.OrderByDescending(p => p.EndDate).ToListAsync(ct);
        Items = all.Select(p => p.ToDto()).ToList();
    }
}
