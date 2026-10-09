using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Services;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.TinTuc;

/// <summary>
/// Admin Blog CREATE — /admin/tin-tuc/them
///
/// Staff-facing flow:
///   1. Render an empty form with category dropdown.
///   2. On POST, validate the staff-editable fields.
///   3. Generate a unique slug from the title (one-shot at create time).
///   4. Persist. Set success TempData ONLY after SaveChangesAsync returns.
///   5. Redirect to /admin/tin-tuc.
///
/// On any save failure, log the actual exception, surface a friendly
/// Vietnamese message, and re-render the form. Success is never claimed
/// before persistence is durable.
/// </summary>
public class TinTucThemModel(
    IRepository<BlogPost> repo,
    IUnitOfWork uow,
    HieuNgaDbContext db,
    ILogger<TinTucThemModel> logger) : PageModel
{
    [BindProperty]
    public BlogPostInputModel Input { get; set; } = new();

    public SelectList CategoryOptions { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm bài viết";
        await LoadCategoriesAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm bài viết";
        await LoadCategoriesAsync(ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Generate a unique slug from the title. Slug uniqueness must
        // match the DB-level unique index which spans ALL rows
        // (including soft-deleted ones), so we bypass the global
        // soft-delete filter here. A failure here becomes a friendly
        // validation error instead of a 23505 race into SaveChanges.
        var slug = SlugHelper.Generate(Input.Title);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = Guid.NewGuid().ToString("N")[..8];
        }

        var slugTaken = await db.BlogPosts
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Slug == slug, ct);
        if (slugTaken)
        {
            ModelState.AddModelError("Input.Title",
                "Tiêu đề này đã được sử dụng cho một bài viết khác. Vui lòng chọn tiêu đề khác.");
            return Page();
        }

        var now = DateTime.UtcNow;
        var entity = new BlogPost
        {
            Title = Input.Title.Trim(),
            Slug = slug,
            Summary = string.IsNullOrWhiteSpace(Input.Summary) ? null : Input.Summary.Trim(),
            Content = Input.Content,
            ThumbnailUrl = string.IsNullOrWhiteSpace(Input.ThumbnailUrl) ? null : Input.ThumbnailUrl,
            CategoryId = Input.CategoryId,
            AuthorName = string.IsNullOrWhiteSpace(Input.AuthorName) ? null : Input.AuthorName.Trim(),
            PublishedAt = Input.PublishedAt ?? (Input.IsPublished ? now : null),
            IsPublished = Input.IsPublished,
            // Server-managed SEO. Staff never edits these.
            MetaTitle = $"{Input.Title.Trim()} | Hiếu Nga",
            MetaDescription = TrimForMeta(Input.Summary ?? Input.Content, 160),
            MetaKeywords = null,
            OgImageUrl = string.IsNullOrWhiteSpace(Input.ThumbnailUrl) ? null : Input.ThumbnailUrl,
            CanonicalUrl = null,
        };

        try
        {
            await repo.AddAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create BlogPost. Title={Title}", Input.Title);
            ModelState.AddModelError(string.Empty,
                "Không thể lưu bài viết. Vui lòng kiểm tra lại thông tin.");
            return Page();
        }

        this.SetSuccess("Đã thêm bài viết");
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

    /// <summary>
    /// Trim a blob to <paramref name="max"/> chars on a word boundary
    /// for use as a MetaDescription. Returns null for null/blank input.
    /// </summary>
    private static string? TrimForMeta(string? input, int max)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim();
        if (s.Length <= max) return s;
        var cut = s.LastIndexOf(' ', Math.Max(0, max - 1));
        if (cut < max / 2) cut = max;
        return s[..cut].TrimEnd() + "…";
    }
}
