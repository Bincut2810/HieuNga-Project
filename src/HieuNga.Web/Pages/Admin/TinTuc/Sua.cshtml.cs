using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.TinTuc;

/// <summary>
/// Admin Blog UPDATE — /admin/tin-tuc/sua/{id}
///
/// Staff-facing flow:
///   1. Load the post (404 if missing or soft-deleted).
///   2. On POST, validate the staff-editable fields.
///   3. Apply ONLY staff-editable fields to the entity:
///        Title, Summary, Content, ThumbnailUrl, CategoryId,
///        AuthorName, PublishedAt, IsPublished.
///   4. NEVER touch Slug, SEO, CreatedAt, IsDeleted, ViewCount.
///      → Title changes MUST NOT break the public URL.
///   5. Save. Set success TempData ONLY after SaveChangesAsync returns.
///   6. Redirect to /admin/tin-tuc.
///
/// On any save failure, log the actual exception, surface a friendly
/// Vietnamese message, and re-render the form.
/// </summary>
public class TinTucSuaModel(
    IRepository<BlogPost> repo,
    IUnitOfWork uow,
    HieuNgaDbContext db,
    ILogger<TinTucSuaModel> logger) : PageModel
{
    [BindProperty]
    public BlogPostInputModel Input { get; set; } = new();

    public SelectList CategoryOptions { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa bài viết";

        // The global query filter hides IsDeleted rows. Use IgnoreQueryFilters
        // only if we want to show a "this was deleted" page — but the staff
        // shouldn't be able to re-edit a soft-deleted post, so we go through
        // the normal repository and 404 on a deleted/missing row.
        var entity = await db.BlogPosts
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (entity is null)
        {
            return NotFound();
        }

        Input = new BlogPostInputModel
        {
            Title = entity.Title,
            Summary = entity.Summary,
            Content = entity.Content,
            ThumbnailUrl = entity.ThumbnailUrl,
            CategoryId = entity.CategoryId,
            AuthorName = entity.AuthorName,
            PublishedAt = entity.PublishedAt,
            IsPublished = entity.IsPublished,
        };
        await LoadCategoriesAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa bài viết";
        await LoadCategoriesAsync(ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var entity = await db.BlogPosts
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (entity is null)
        {
            return NotFound();
        }

        // ─── Apply ONLY staff-editable fields. ───────────────
        // The system fields below MUST NOT be touched:
        //   Id, Slug, MetaTitle, MetaDescription, MetaKeywords,
        //   OgImageUrl, CanonicalUrl, CreatedAt, IsDeleted, ViewCount.
        entity.Title = Input.Title.Trim();
        entity.Summary = string.IsNullOrWhiteSpace(Input.Summary) ? null : Input.Summary.Trim();
        entity.Content = Input.Content;

        // Thumbnail: treat empty/whitespace as "keep current", only
        // overwrite when the staff provided a non-empty value.
        if (!string.IsNullOrWhiteSpace(Input.ThumbnailUrl))
        {
            entity.ThumbnailUrl = Input.ThumbnailUrl;
        }

        entity.CategoryId = Input.CategoryId;
        entity.AuthorName = string.IsNullOrWhiteSpace(Input.AuthorName) ? null : Input.AuthorName.Trim();

        // PublishedAt policy:
        //   - If staff supplied a date, use it.
        //   - Else if staff is publishing for the first time, set now.
        //   - Else (unpublishing or no change), preserve existing.
        if (Input.PublishedAt.HasValue)
        {
            entity.PublishedAt = Input.PublishedAt.Value;
        }
        else if (Input.IsPublished && !entity.PublishedAt.HasValue)
        {
            entity.PublishedAt = DateTime.UtcNow;
        }
        // else: keep the existing entity.PublishedAt intact.

        entity.IsPublished = Input.IsPublished;

        try
        {
            await repo.UpdateAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update BlogPost. Id={Id}", id);
            ModelState.AddModelError(string.Empty,
                "Không thể lưu bài viết. Vui lòng kiểm tra lại thông tin.");
            return Page();
        }

        this.SetSuccess("Đã cập nhật bài viết");
        return RedirectToPage("./Index");
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        var cats = await db.BlogCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        CategoryOptions = new SelectList(cats, "Id", "Name", Input.CategoryId);
        ViewData["CategoryOptions"] = CategoryOptions;
    }
}
