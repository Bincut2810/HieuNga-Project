using HieuNga.Application.Mappings;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.TinTuc;

public class IndexModel(
    HieuNgaDbContext db,
    IRepository<BlogPost> repo,
    IUnitOfWork uow) : PageModel
{
    public IReadOnlyList<Row> Posts { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    /// <summary>
    /// Admin-only row that extends the shared
    /// <see cref="HieuNga.Application.DTOs.BlogPostListItemDto"/> with the
    /// category name for the listing. We do NOT add CategoryName to the
    /// shared DTO because the public site must stay unchanged; this is
    /// strictly an Admin CMS concern.
    /// </summary>
    public record Row(
        Guid Id,
        string Title,
        string? ThumbnailUrl,
        DateTime? PublishedAt,
        bool IsPublished,
        string? CategoryName);

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Tin tức";
        var query = db.BlogPosts.AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(p => p.Title.Contains(Search) || (p.Slug != null && p.Slug.Contains(Search)));

        // Project directly into the admin Row so we can include the
        // category name. AsNoTracking avoids polluting the change tracker
        // and matches the previous public-DTO query.
        var rows = await query
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.ThumbnailUrl,
                p.PublishedAt,
                p.IsPublished,
                CategoryName = p.Category != null ? p.Category.Name : null
            })
            .ToListAsync(ct);
        Posts = rows.Select(r => new Row(
            r.Id, r.Title, r.ThumbnailUrl, r.PublishedAt, r.IsPublished, r.CategoryName)).ToList();
    }

    /// <summary>
    /// Delete from the listing. Phase 3 — this is the SINGLE canonical
    /// delete flow for Blog posts. There is no Xoa.cshtml and no
    /// edit-page delete handler. The staff always deletes from
    /// /admin/tin-tuc via the per-row [Xóa] button.
    ///
    /// Persistence is verified — SaveChangesAsync only returns when
    /// the soft-delete row is durably committed; the success message
    /// is written AFTER.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa bài viết.");
        return RedirectToPage(new { Search });
    }
}
