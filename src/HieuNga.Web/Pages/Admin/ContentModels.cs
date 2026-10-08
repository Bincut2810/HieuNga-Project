using System.ComponentModel.DataAnnotations;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Services;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin;

public class BranchInputModel
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    [Required] public string Address { get; set; } = string.Empty;
    public string? District { get; set; }
    public string City { get; set; } = "Đà Nẵng";
    public string? Phone { get; set; }
    public string? Hotline { get; set; }
    public string? Email { get; set; }
    public string? MapEmbedUrl { get; set; }
    public string? OpeningHours { get; set; }
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class PromotionInputModel : IAdminSeoInput
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public PromotionType Type { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    [Required] public DateTime StartDate { get; set; } = DateTime.Today;
    [Required] public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public Guid? MotorcycleId { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }
}

public class BlogPostInputModel : IAdminSeoInput
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public string? AuthorName { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }
}

public class ChiNhanhThemModel(IRepository<Branch> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty] public BranchInputModel Input { get; set; } = new();

    public void OnGet() => ViewData["Title"] = "Thêm chi nhánh";

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm chi nhánh";
        if (!ModelState.IsValid) return Page();
        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Name) : SlugHelper.Generate(Input.Slug);
        if (await db.Branches.AnyAsync(b => b.Slug == slug && !b.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }
        await repo.AddAsync(Map(new Branch(), Input, slug), ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm chi nhánh.");
        return RedirectToPage("/Admin/ChiNhanh/Index");
    }

    internal static Branch Map(Branch e, BranchInputModel i, string slug)
    {
        e.Name = i.Name.Trim(); e.Slug = slug; e.Address = i.Address; e.District = i.District;
        e.City = i.City; e.Phone = i.Phone; e.Hotline = i.Hotline; e.Email = i.Email;
        e.MapEmbedUrl = i.MapEmbedUrl; e.OpeningHours = i.OpeningHours;
        e.IsHeadOffice = i.IsHeadOffice; e.IsActive = i.IsActive; e.SortOrder = i.SortOrder;
        return e;
    }
}

public class ChiNhanhSuaModel(IRepository<Branch> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty] public BranchInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa chi nhánh";
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        Input = new BranchInputModel
        {
            Name = e.Name, Slug = e.Slug, Address = e.Address, District = e.District, City = e.City,
            Phone = e.Phone, Hotline = e.Hotline, Email = e.Email, MapEmbedUrl = e.MapEmbedUrl,
            OpeningHours = e.OpeningHours, IsHeadOffice = e.IsHeadOffice, IsActive = e.IsActive, SortOrder = e.SortOrder
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa chi nhánh";
        if (!ModelState.IsValid) return Page();
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Name) : SlugHelper.Generate(Input.Slug);
        if (await db.Branches.AnyAsync(b => b.Slug == slug && b.Id != id && !b.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }
        ChiNhanhThemModel.Map(e, Input, slug);
        await repo.UpdateAsync(e, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật chi nhánh.");
        return RedirectToPage("/Admin/ChiNhanh/Index");
    }
}

public class KhuyenMaiThemModel(IRepository<Promotion> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty] public PromotionInputModel Input { get; set; } = new();
    public SelectList MotorcycleOptions { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm khuyến mãi";
        await LoadMotorcyclesAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm khuyến mãi";
        await LoadMotorcyclesAsync(ct);
        if (!ModelState.IsValid) return Page();
        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Title) : SlugHelper.Generate(Input.Slug);
        if (await db.Promotions.AnyAsync(p => p.Slug == slug && !p.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }
        await repo.AddAsync(Map(new Promotion(), Input, slug), ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm khuyến mãi.");
        return RedirectToPage("/Admin/KhuyenMai/Index");
    }

    private async Task LoadMotorcyclesAsync(CancellationToken ct)
    {
        var bikes = await db.Motorcycles.AsNoTracking().Where(m => !m.IsDeleted).OrderBy(m => m.Name).ToListAsync(ct);
        MotorcycleOptions = new SelectList(bikes, "Id", "Name", Input.MotorcycleId);
        ViewData["MotorcycleOptions"] = MotorcycleOptions;
    }

    internal static Promotion Map(Promotion e, PromotionInputModel i, string slug)
    {
        e.Title = i.Title.Trim(); e.Slug = slug; e.Summary = i.Summary; e.Content = i.Content; e.Type = i.Type;
        e.DiscountPercent = i.DiscountPercent; e.DiscountAmount = i.DiscountAmount;
        e.StartDate = i.StartDate; e.EndDate = i.EndDate; e.ImageUrl = i.ImageUrl;
        e.IsActive = i.IsActive; e.IsFeatured = i.IsFeatured; e.MotorcycleId = i.MotorcycleId;
        e.MetaTitle = i.MetaTitle; e.MetaDescription = i.MetaDescription; e.MetaKeywords = i.MetaKeywords;
        e.OgImageUrl = i.OgImageUrl; e.CanonicalUrl = i.CanonicalUrl;
        return e;
    }
}

public class KhuyenMaiSuaModel(IRepository<Promotion> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty] public PromotionInputModel Input { get; set; } = new();
    public SelectList MotorcycleOptions { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa khuyến mãi";
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        Input = new PromotionInputModel
        {
            Title = e.Title, Slug = e.Slug, Summary = e.Summary, Content = e.Content, Type = e.Type,
            DiscountPercent = e.DiscountPercent, DiscountAmount = e.DiscountAmount,
            StartDate = e.StartDate, EndDate = e.EndDate, ImageUrl = e.ImageUrl,
            IsActive = e.IsActive, IsFeatured = e.IsFeatured, MotorcycleId = e.MotorcycleId,
            MetaTitle = e.MetaTitle, MetaDescription = e.MetaDescription, MetaKeywords = e.MetaKeywords,
            OgImageUrl = e.OgImageUrl, CanonicalUrl = e.CanonicalUrl
        };
        await LoadMotorcyclesAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa khuyến mãi";
        await LoadMotorcyclesAsync(ct);
        if (!ModelState.IsValid) return Page();
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Title) : SlugHelper.Generate(Input.Slug);
        if (await db.Promotions.AnyAsync(p => p.Slug == slug && p.Id != id && !p.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }
        KhuyenMaiThemModel.Map(e, Input, slug);
        await repo.UpdateAsync(e, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật khuyến mãi.");
        return RedirectToPage("/Admin/KhuyenMai/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Khuyến mãi";
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        e.IsDeleted = true;
        e.IsActive = false;
        e.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(e, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa khuyến mãi.");
        return RedirectToPage("/Admin/KhuyenMai/Index");
    }

    private async Task LoadMotorcyclesAsync(CancellationToken ct)
    {
        var bikes = await db.Motorcycles.AsNoTracking().Where(m => !m.IsDeleted).OrderBy(m => m.Name).ToListAsync(ct);
        MotorcycleOptions = new SelectList(bikes, "Id", "Name", Input.MotorcycleId);
        ViewData["MotorcycleOptions"] = MotorcycleOptions;
    }
}

public class TinTucThemModel(IRepository<BlogPost> repo, IUnitOfWork uow, HieuNgaDbContext db, ILogger<TinTucThemModel> logger) : PageModel
{
    [BindProperty] public BlogPostInputModel Input { get; set; } = new();
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
        if (!ModelState.IsValid) return Page();

        // Phase 2 — staff don't edit Slug/SEO. Always derive slug from the
        // Title on CREATE; the SEO fields are auto-populated below.
        var slug = SlugHelper.Generate(Input.Title);
        if (string.IsNullOrWhiteSpace(slug))
            slug = Guid.NewGuid().ToString("N")[..8];

        // Slug uniqueness must include soft-deleted rows too: the
        // BlogPostConfiguration has a unique index on Slug which spans
        // ALL rows including soft-deleted ones at the DB level. A check
        // that hides soft-deleted rows from the lookup would let a
        // duplicate slip through and the subsequent SaveChangesAsync
        // would 500 in production (Npgsql unique violation 23505). Use
        // IgnoreQueryFilters() to bypass the global soft-delete filter
        // so the lookup matches the DB-level unique index.
        var collision = await db.BlogPosts
            .IgnoreQueryFilters()
            .Where(p => p.Slug == slug)
            .Select(p => new { p.Id, p.IsDeleted })
            .FirstOrDefaultAsync(ct);
        if (collision is not null)
        {
            if (collision.IsDeleted)
            {
                ModelState.AddModelError("Input.Title", "Tiêu đề này trùng với một bài viết đã xóa trước đó. Vui lòng đổi tiêu đề khác.");
            }
            else
            {
                ModelState.AddModelError("Input.Title", "Slug đã tồn tại. Vui lòng đổi tiêu đề khác.");
            }
            return Page();
        }

        var entity = new BlogPost
        {
            Title = Input.Title.Trim(),
            Slug = slug,
            Summary = Input.Summary,
            Content = Input.Content,
            ThumbnailUrl = Input.ThumbnailUrl,
            CategoryId = Input.CategoryId,
            AuthorName = Input.AuthorName,
            PublishedAt = Input.PublishedAt ?? (Input.IsPublished ? DateTime.UtcNow : null),
            IsPublished = Input.IsPublished,
            // Phase 2 — staff don't manage SEO fields. Auto-seed them from
            // Title so the public website still has meaningful SEO
            // metadata on first creation without requiring staff input.
            MetaTitle = $"{(Input.Title ?? string.Empty).Trim()} | Hiếu Nga",
            MetaDescription = TrimForMeta(Input.Summary ?? Input.Content, 160),
            MetaKeywords = null,
            OgImageUrl = Input.ThumbnailUrl,
            CanonicalUrl = null,
        };
        await repo.AddAsync(entity, ct);

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Last-line defense: even with the application-level pre-check
            // above, a concurrent insert could race past it. Surface a
            // friendly message instead of 500-ing.
            logger.LogWarning(ex, "BlogPost slug unique violation on create. Slug={Slug}", slug);
            ModelState.AddModelError("Input.Title", "Slug đã tồn tại (trùng với bài viết đã xóa). Vui lòng đổi tiêu đề khác.");
            return Page();
        }

        this.SetSuccess("Đã thêm bài viết.");
        return RedirectToPage("/Admin/TinTuc/Index");
    }

    internal static BlogPost Map(BlogPost e, BlogPostInputModel i, string slug)
    {
        e.Title = i.Title.Trim();
        e.Slug = slug;
        e.Summary = i.Summary;
        e.Content = i.Content;
        e.ThumbnailUrl = i.ThumbnailUrl;
        e.CategoryId = i.CategoryId;
        e.AuthorName = i.AuthorName;
        e.PublishedAt = i.PublishedAt ?? (i.IsPublished ? DateTime.UtcNow : null);
        e.IsPublished = i.IsPublished;
        e.MetaTitle = i.MetaTitle;
        e.MetaDescription = i.MetaDescription;
        e.MetaKeywords = i.MetaKeywords;
        e.OgImageUrl = i.OgImageUrl;
        e.CanonicalUrl = i.CanonicalUrl;
        return e;
    }

    /// <summary>
    /// Merge the staff-editable fields from <paramref name="i"/> onto
    /// <paramref name="e"/> without touching SEO/Slug/PublishedAt fields
    /// that the staff form does not post. Phase 2: the staff form only
    /// posts Title/Summary/Content/CategoryId/AuthorName/IsPublished/
    /// PublishedAt/ThumbnailUrl; everything else (Slug + SEO + timestamp
    /// policy) is preserved or auto-managed by the server.
    /// </summary>
    internal static void MergeEditableFields(BlogPost e, BlogPostInputModel i)
    {
        e.Title = i.Title.Trim();
        e.Summary = i.Summary;
        e.Content = i.Content;
        e.CategoryId = i.CategoryId;
        e.AuthorName = i.AuthorName;

        // Thumbnail: only overwrite when the form explicitly carries a
        // value (the image uploader renders a hidden input whose name
        // matches the bind target). An empty/null posted value would
        // erase the previously-stored thumbnail.
        if (i.ThumbnailUrl is not null)
            e.ThumbnailUrl = string.IsNullOrWhiteSpace(i.ThumbnailUrl) ? null : i.ThumbnailUrl;

        // Publish state + timestamp policy:
        //  - If the staff toggled IsPublished from draft → published, set
        //    PublishedAt only if it isn't already set.
        //  - If the staff toggled published → draft, KEEP the prior
        //    PublishedAt so the article can be restored later without
        //    losing the original publication date.
        e.IsPublished = i.IsPublished;
        if (i.PublishedAt.HasValue)
        {
            e.PublishedAt = i.PublishedAt.Value;
        }
        else if (i.IsPublished && !e.PublishedAt.HasValue)
        {
            e.PublishedAt = DateTime.UtcNow;
        }
        // else: keep existing e.PublishedAt as-is.
    }

    /// <summary>
    /// Returns true if <paramref name="ex"/> is a Npgsql / PostgreSQL
    /// unique-constraint violation (SQLSTATE 23505). Used to translate
    /// a DB-level race past the application-level pre-check into a
    /// friendly ModelState error rather than a 500.
    /// </summary>
    internal static bool IsUniqueViolation(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException!)
        {
            if (e is Npgsql.PostgresException pg && pg.SqlState == "23505") return true;
        }
        return false;
    }

    /// <summary>
    /// Trim a blob to <paramref name="max"/> chars on a word boundary
    /// for use as a MetaDescription. Returns null for null/blank input.
    /// </summary>
    internal static string? TrimForMeta(string? input, int max)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim();
        if (s.Length <= max) return s;
        var cut = s.LastIndexOf(' ', Math.Max(0, max - 1));
        if (cut < max / 2) cut = max;
        return s[..cut].TrimEnd() + "…";
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        var cats = await db.BlogCategories.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync(ct);
        CategoryOptions = new SelectList(cats, "Id", "Name", Input.CategoryId);
        ViewData["CategoryOptions"] = CategoryOptions;
    }
}

public class TinTucSuaModel(IRepository<BlogPost> repo, IUnitOfWork uow, HieuNgaDbContext db, ILogger<TinTucSuaModel> logger) : PageModel
{
    [BindProperty] public BlogPostInputModel Input { get; set; } = new();
    public SelectList CategoryOptions { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa bài viết";
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        Input = new BlogPostInputModel
        {
            // Phase 2 — staff don't manage Slug/SEO. We expose ONLY the
            // fields the staff form actually posts, but keep SEO/Slug
            // initialized on the entity side so they survive a round-trip
            // and don't get nulled by the merge below.
            Title = e.Title,
            Summary = e.Summary,
            Content = e.Content,
            ThumbnailUrl = e.ThumbnailUrl,
            CategoryId = e.CategoryId,
            AuthorName = e.AuthorName,
            PublishedAt = e.PublishedAt,
            IsPublished = e.IsPublished,
        };
        await LoadCategoriesAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa bài viết";
        await LoadCategoriesAsync(ct);

        // Required-field validation. SEO/Slug/Category are NOT in the
        // staff form, so they don't appear in ModelState and never fail
        // validation here.
        if (!ModelState.IsValid) return Page();

        // Allow pages that already exist to be updated even if their
        // entity is missing/soft-deleted (returning 404 here would
        // re-fail the staff's edit and force them to re-create the post).
        var e = await db.BlogPosts
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (e is null || e.IsDeleted) return NotFound();

        // Phase 2 — staff don't edit Slug. Preserve the existing slug so
        // the public URL doesn't 404 every time the title is touched.
        // Only re-generate if the slug is missing/blank (data-recovery).
        var existingSlug = (e.Slug ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(existingSlug))
        {
            existingSlug = SlugHelper.Generate(Input.Title);
            if (string.IsNullOrWhiteSpace(existingSlug))
                existingSlug = Guid.NewGuid().ToString("N")[..8];
        }

        // Slug uniqueness must include soft-deleted rows too: the
        // BlogPostConfiguration has a unique index on Slug which spans
        // ALL rows including soft-deleted ones at the DB level. A check
        // that hides soft-deleted rows from the lookup would let a
        // duplicate slip through and the subsequent SaveChangesAsync
        // would 500 in production (Npgsql unique violation 23505). For
        // UPDATE we only need to compare against OTHER records
        // (different Id), but we still must exclude self when comparing
        // — use IgnoreQueryFilters() to bypass the global soft-delete
        // filter so the lookup matches the DB-level unique index.
        var slugCollision = await db.BlogPosts
            .IgnoreQueryFilters()
            .Where(p => p.Slug == existingSlug && p.Id != id)
            .Select(p => new { p.Id, p.IsDeleted })
            .FirstOrDefaultAsync(ct);
        if (slugCollision is not null)
        {
            // Extremely unlikely in practice (only fires after a slug
            // was reused post-soft-delete). Surface a clear message so
            // the staff can adjust the title.
            ModelState.AddModelError("Input.Title",
                slugCollision.IsDeleted
                    ? "Slug này trùng với một bài viết đã xóa. Vui lòng đổi tiêu đề khác."
                    : "Slug này đã được dùng cho bài viết khác. Vui lòng đổi tiêu đề khác.");
            return Page();
        }

        // Phase 2 — only the staff-editable fields are merged. SEO,
        // Slug, and ViewCount are intentionally preserved.
        TinTucThemModel.MergeEditableFields(e, Input);

        await repo.UpdateAsync(e, ct);

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (TinTucThemModel.IsUniqueViolation(ex))
        {
            // Last-line defense: a concurrent insert that created a row
            // with the same slug between our pre-check and SaveChanges
            // would otherwise 500 the staff. Translate into a friendly
            // model error.
            logger.LogWarning(ex, "BlogPost slug unique violation on update. Id={Id} Slug={Slug}",
                id, existingSlug);
            ModelState.AddModelError("Input.Title",
                "Slug này trùng với một bài viết khác (vừa được thêm). Vui lòng đổi tiêu đề.");
            return Page();
        }

        this.SetSuccess("Đã cập nhật bài viết.");
        return RedirectToPage("/Admin/TinTuc/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Tin tức";
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        e.IsDeleted = true;
        e.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(e, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa bài viết.");
        return RedirectToPage("/Admin/TinTuc/Index");
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        var cats = await db.BlogCategories.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync(ct);
        CategoryOptions = new SelectList(cats, "Id", "Name", Input.CategoryId);
        ViewData["CategoryOptions"] = CategoryOptions;
    }
}
