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

public class BannerInputModel
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    [Required] public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryCtaUrl { get; set; }
    public string? Badge { get; set; }
    [Range(0, 100)] public int OverlayStrength { get; set; } = 65;
    public BannerTextAlignment TextAlignment { get; set; } = BannerTextAlignment.Left;
    public BannerPosition Position { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
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

public class BannerThemModel(IRepository<Domain.Entities.Banner> repo, IUnitOfWork uow) : PageModel
{
    [BindProperty] public BannerInputModel Input { get; set; } = new();
    public void OnGet() => ViewData["Title"] = "Thêm banner";
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm banner";
        if (!ModelState.IsValid) return Page();
        await repo.AddAsync(Map(new Domain.Entities.Banner(), Input), ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm banner.");
        return RedirectToPage("/Admin/Banner/Index");
    }
    internal static Domain.Entities.Banner Map(Domain.Entities.Banner e, BannerInputModel i)
    {
        e.Title = i.Title;
        e.Subtitle = i.Subtitle;
        e.ImageUrl = i.ImageUrl;
        e.MobileImageUrl = i.MobileImageUrl;
        e.CtaText = i.CtaText;
        e.CtaUrl = i.CtaUrl;
        e.SecondaryCtaText = i.SecondaryCtaText;
        e.SecondaryCtaUrl = i.SecondaryCtaUrl;
        e.Badge = i.Badge;
        e.OverlayStrength = Math.Clamp(i.OverlayStrength, 0, 100);
        e.TextAlignment = i.TextAlignment;
        e.Position = i.Position;
        e.SortOrder = i.SortOrder;
        e.IsActive = i.IsActive;
        e.StartDate = i.StartDate;
        e.EndDate = i.EndDate;
        return e;
    }
}

public class BannerSuaModel(IRepository<Domain.Entities.Banner> repo, IUnitOfWork uow) : PageModel
{
    [BindProperty] public BannerInputModel Input { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa banner";
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        Input = new BannerInputModel
        {
            Title = e.Title,
            Subtitle = e.Subtitle,
            ImageUrl = e.ImageUrl,
            MobileImageUrl = e.MobileImageUrl,
            CtaText = e.CtaText,
            CtaUrl = e.CtaUrl,
            SecondaryCtaText = e.SecondaryCtaText,
            SecondaryCtaUrl = e.SecondaryCtaUrl,
            Badge = e.Badge,
            OverlayStrength = e.OverlayStrength,
            TextAlignment = e.TextAlignment,
            Position = e.Position,
            SortOrder = e.SortOrder,
            IsActive = e.IsActive,
            StartDate = e.StartDate,
            EndDate = e.EndDate
        };
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa banner";
        if (!ModelState.IsValid) return Page();
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        BannerThemModel.Map(e, Input);
        await repo.UpdateAsync(e, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật banner.");
        return RedirectToPage("/Admin/Banner/Index");
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

    private async Task LoadMotorcyclesAsync(CancellationToken ct)
    {
        var bikes = await db.Motorcycles.AsNoTracking().Where(m => !m.IsDeleted).OrderBy(m => m.Name).ToListAsync(ct);
        MotorcycleOptions = new SelectList(bikes, "Id", "Name", Input.MotorcycleId);
        ViewData["MotorcycleOptions"] = MotorcycleOptions;
    }
}

public class TinTucThemModel(IRepository<BlogPost> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
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
        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Title) : SlugHelper.Generate(Input.Slug);
        if (await db.BlogPosts.AnyAsync(p => p.Slug == slug && !p.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }
        await repo.AddAsync(Map(new BlogPost(), Input, slug), ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm bài viết.");
        return RedirectToPage("/Admin/TinTuc/Index");
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        var cats = await db.BlogCategories.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync(ct);
        CategoryOptions = new SelectList(cats, "Id", "Name", Input.CategoryId);
        ViewData["CategoryOptions"] = CategoryOptions;
    }

    internal static BlogPost Map(BlogPost e, BlogPostInputModel i, string slug)
    {
        e.Title = i.Title.Trim(); e.Slug = slug; e.Summary = i.Summary; e.Content = i.Content;
        e.ThumbnailUrl = i.ThumbnailUrl; e.CategoryId = i.CategoryId; e.AuthorName = i.AuthorName;
        e.PublishedAt = i.PublishedAt ?? (i.IsPublished ? DateTime.UtcNow : null);
        e.IsPublished = i.IsPublished;
        e.MetaTitle = i.MetaTitle; e.MetaDescription = i.MetaDescription; e.MetaKeywords = i.MetaKeywords;
        e.OgImageUrl = i.OgImageUrl; e.CanonicalUrl = i.CanonicalUrl;
        return e;
    }
}

public class TinTucSuaModel(IRepository<BlogPost> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
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
            Title = e.Title, Slug = e.Slug, Summary = e.Summary, Content = e.Content,
            ThumbnailUrl = e.ThumbnailUrl, CategoryId = e.CategoryId, AuthorName = e.AuthorName,
            PublishedAt = e.PublishedAt, IsPublished = e.IsPublished,
            MetaTitle = e.MetaTitle, MetaDescription = e.MetaDescription, MetaKeywords = e.MetaKeywords,
            OgImageUrl = e.OgImageUrl, CanonicalUrl = e.CanonicalUrl
        };
        await LoadCategoriesAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa bài viết";
        await LoadCategoriesAsync(ct);
        if (!ModelState.IsValid) return Page();
        var e = await repo.GetByIdAsync(id, ct);
        if (e is null || e.IsDeleted) return NotFound();
        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Title) : SlugHelper.Generate(Input.Slug);
        if (await db.BlogPosts.AnyAsync(p => p.Slug == slug && p.Id != id && !p.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }
        TinTucThemModel.Map(e, Input, slug);
        await repo.UpdateAsync(e, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật bài viết.");
        return RedirectToPage("/Admin/TinTuc/Index");
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        var cats = await db.BlogCategories.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync(ct);
        CategoryOptions = new SelectList(cats, "Id", "Name", Input.CategoryId);
        ViewData["CategoryOptions"] = CategoryOptions;
    }
}
