using HieuNga.Application.Mappings;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.TinTuc;

public class IndexModel(HieuNgaDbContext db) : PageModel
{
    public IReadOnlyList<Application.DTOs.BlogPostListItemDto> Posts { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Tin tức";
        var query = db.BlogPosts.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(p => p.Title.Contains(Search) || (p.Slug != null && p.Slug.Contains(Search)));

        var all = await query
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .ToListAsync(ct);
        Posts = all.Select(p => p.ToListItem()).ToList();
    }
}
