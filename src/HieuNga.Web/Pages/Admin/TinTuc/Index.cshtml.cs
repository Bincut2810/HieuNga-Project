using HieuNga.Application.Mappings;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.TinTuc;

public class IndexModel(HieuNgaDbContext db) : PageModel
{
    public IReadOnlyList<Application.DTOs.BlogPostListItemDto> Posts { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Tin tức";
        var all = await db.BlogPosts.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .ToListAsync(ct);
        Posts = all.Select(p => p.ToListItem()).ToList();
    }
}
