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
    IUnitOfWork uow,
    ILogger<IndexModel> logger) : PageModel
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
    /// Single canonical delete flow for Blog posts — there is no Xoa.cshtml
    /// and no edit-page delete handler. The staff always deletes from
    /// <c>/admin/tin-tuc</c> via the per-row [Xóa] button.
    ///
    /// Rules:
    /// <list type="bullet">
    ///   <item><b>Route / antiforgery</b>: <c>asp-page-handler="Delete"</c>
    ///   plus <c>[ValidateAntiForgeryToken]</c> via the layout pipeline.</item>
    ///   <item><b>Confirmation</b>: the JS modal in
    ///   <c>wwwroot/js/admin-toast.js</c> intercepts the click and posts
    ///   only after the staff confirms.</item>
    ///   <item><b>Soft delete only</b>: never physically delete database
    ///   rows. The global EF query filter hides <c>IsDeleted</c> rows.</item>
    ///   <item><b>UTC</b>: <c>UpdatedAt</c> is set in UTC by
    ///   <see cref="Repository{T}.UpdateAsync"/>.</item>
    ///   <item><b>No false success</b>: the success TempData is written
    ///   ONLY after <c>SaveChangesAsync</c> returns successfully. A caught
    ///   persistence failure logs and surfaces a friendly error to the
    ///   staff.</item>
    ///   <item><b>Search state preserved</b>: a successful delete
    ///   redirects back to the list and keeps the active <c>Search</c>
    ///   filter so the staff doesn't lose their place.</item>
    /// </list>
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        // 1. Load the existing non-deleted entity. Missing IDs and
        //    already-deleted rows both surface as 404 — same response so
        //    we don't leak the existence of soft-deleted records.
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted)
        {
            return NotFound();
        }

        // 2. Soft-delete via the shared convention. UpdateAsync also
        //    stamps UpdatedAt = DateTime.UtcNow (UTC) under the hood.
        entity.IsDeleted = true;

        try
        {
            await repo.UpdateAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // 3. Failure path: log + show a friendly VI error; do NOT
            //    claim success. The entity may be tracked in Unchanged/
            //    Modified state across the failed SaveChanges, so detach
            //    it to keep a follow-up POST clean.
            logger.LogError(ex, "Failed to soft-delete BlogPost. Id={Id}", id);
            if (db.Entry(entity).State != EntityState.Detached)
            {
                db.Entry(entity).State = EntityState.Detached;
            }
            this.SetError("Không thể xóa bài viết. Vui lòng thử lại.");
            return RedirectToPage(new { Search });
        }

        // 4. Success path.
        this.SetSuccess("Đã xóa bài viết.");
        return RedirectToPage(new { Search });
    }
}
