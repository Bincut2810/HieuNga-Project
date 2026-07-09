using System.ComponentModel.DataAnnotations;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Services;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.DichVu;

public class ServiceItemInputModel : IAdminSeoInput
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public Guid ServiceCategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Slug { get; set; }

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    public string? DetailDescription { get; set; }
    public string? IncludesLines { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá tham khảo")]
    public string EstimatedPriceText { get; set; } = string.Empty;

    public string? EstimatedDurationText { get; set; }
    public string? PriceNote { get; set; }
    public string? IconKey { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    [StringLength(300)]
    public string? MetaKeywords { get; set; }

    [StringLength(500)]
    public string? OgImageUrl { get; set; }

    [StringLength(500)]
    public string? CanonicalUrl { get; set; }
}

public class BangGiaIndexModel(IRepository<ServiceItem> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public IReadOnlyList<Row> Items { get; private set; } = [];
    public SelectList CategoryOptions { get; private set; } = null!;
    public bool HasActiveFilters => CategoryId.HasValue || !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Status);

    public record Row(
        Guid Id,
        string Name,
        string Slug,
        string Category,
        string Price,
        string? Duration,
        bool IsActive,
        DateTime? UpdatedAt);

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Bảng giá dịch vụ";
        await LoadPageAsync(ct);
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(
        Guid id,
        Guid? categoryId,
        string? search,
        string? status,
        CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();

        entity.IsActive = !entity.IsActive;
        await repo.UpdateAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess(entity.IsActive ? "Đã hiển thị dịch vụ." : "Đã ẩn dịch vụ.");
        return RedirectToPage(new { categoryId, search, status });
    }

    private async Task LoadPageAsync(CancellationToken ct)
    {
        var categories = await db.ServiceCategories.AsNoTracking()
            .Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync(ct);
        CategoryOptions = new SelectList(categories, "Id", "Name", CategoryId);

        var q = db.ServiceItems.AsNoTracking().Include(s => s.Category)
            .Where(s => !s.IsDeleted);

        if (CategoryId.HasValue)
            q = q.Where(s => s.ServiceCategoryId == CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            q = q.Where(s => EF.Functions.ILike(s.Name, $"%{term}%"));
        }

        if (Status == "active")
            q = q.Where(s => s.IsActive);
        else if (Status == "inactive")
            q = q.Where(s => !s.IsActive);

        Items = await q.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .Select(s => new Row(
                s.Id,
                s.Name,
                s.Slug,
                s.Category.Name,
                s.EstimatedPriceText,
                s.EstimatedDurationText,
                s.IsActive,
                s.UpdatedAt))
            .ToListAsync(ct);
    }
}

public class BangGiaThemModel(IRepository<ServiceItem> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty]
    public ServiceItemInputModel Input { get; set; } = new();

    public SelectList CategoryOptions { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm dịch vụ";
        await LoadCategoriesAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(string? submitAction, CancellationToken ct)
    {
        ViewData["Title"] = "Thêm dịch vụ";
        await LoadCategoriesAsync(ct);
        NormalizeInput();
        if (!ModelState.IsValid) return Page();

        if (!await CategoryExistsAsync(Input.ServiceCategoryId, ct))
        {
            ModelState.AddModelError("Input.ServiceCategoryId", "Danh mục không hợp lệ.");
            return Page();
        }

        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Name) : SlugHelper.Generate(Input.Slug);
        if (await db.ServiceItems.AnyAsync(s => s.Slug == slug && !s.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }

        var entity = MapToEntity(new ServiceItem(), Input, slug);
        await repo.AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm dịch vụ.");

        if (submitAction == "continue")
            return RedirectToPage("/Admin/DichVu/BangGia/Sua", new { id = entity.Id });

        return RedirectToPage("/Admin/DichVu/BangGia/Index");
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        var cats = await db.ServiceCategories.AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(ct);
        CategoryOptions = new SelectList(cats, "Id", "Name", Input.ServiceCategoryId);
        ViewData["CategoryOptions"] = CategoryOptions;
    }

    internal static void NormalizeInput(ServiceItemInputModel input)
    {
        input.Name = input.Name.Trim();
        input.Slug = string.IsNullOrWhiteSpace(input.Slug) ? null : input.Slug.Trim();
        input.EstimatedPriceText = input.EstimatedPriceText.Trim();
        input.EstimatedDurationText = string.IsNullOrWhiteSpace(input.EstimatedDurationText) ? null : input.EstimatedDurationText.Trim();
        input.ShortDescription = string.IsNullOrWhiteSpace(input.ShortDescription) ? null : input.ShortDescription.Trim();
        input.DetailDescription = string.IsNullOrWhiteSpace(input.DetailDescription) ? null : input.DetailDescription.Trim();
        input.PriceNote = string.IsNullOrWhiteSpace(input.PriceNote) ? null : input.PriceNote.Trim();
        input.IconKey = string.IsNullOrWhiteSpace(input.IconKey) ? null : input.IconKey.Trim();
    }

    private void NormalizeInput() => NormalizeInput(Input);

    internal static Task<bool> CategoryExistsAsync(HieuNgaDbContext db, Guid categoryId, CancellationToken ct) =>
        db.ServiceCategories.AnyAsync(c => c.Id == categoryId && !c.IsDeleted, ct);

    private Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken ct) =>
        CategoryExistsAsync(db, categoryId, ct);

    internal static ServiceItem MapToEntity(ServiceItem entity, ServiceItemInputModel input, string slug)
    {
        entity.ServiceCategoryId = input.ServiceCategoryId;
        entity.Name = input.Name.Trim();
        entity.Slug = slug;
        entity.ShortDescription = input.ShortDescription;
        entity.DetailDescription = input.DetailDescription;
        entity.IncludesJson = ServiceItemJson.SerializeIncludes(
            (input.IncludesLines ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        entity.EstimatedPriceText = input.EstimatedPriceText.Trim();
        entity.EstimatedDurationText = input.EstimatedDurationText;
        entity.PriceNote = input.PriceNote;
        entity.IconKey = input.IconKey;
        entity.DisplayOrder = input.DisplayOrder;
        entity.IsFeatured = input.IsFeatured;
        entity.IsActive = input.IsActive;
        entity.MetaTitle = input.MetaTitle;
        entity.MetaDescription = input.MetaDescription;
        entity.MetaKeywords = input.MetaKeywords;
        entity.OgImageUrl = input.OgImageUrl;
        entity.CanonicalUrl = input.CanonicalUrl;
        return entity;
    }
}

public class BangGiaSuaModel(IRepository<ServiceItem> repo, IUnitOfWork uow, HieuNgaDbContext db) : PageModel
{
    [BindProperty]
    public ServiceItemInputModel Input { get; set; } = new();

    public SelectList CategoryOptions { get; private set; } = null!;
    public string CategoryName { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa dịch vụ";
        var entity = await db.ServiceItems.AsNoTracking()
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (entity is null) return NotFound();

        Input = MapFromEntity(entity);
        CategoryName = entity.Category.Name;
        await LoadCategoriesAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string? submitAction, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa dịch vụ";
        await LoadCategoriesAsync(ct);
        BangGiaThemModel.NormalizeInput(Input);
        if (!ModelState.IsValid) return Page();

        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();

        if (!await BangGiaThemModel.CategoryExistsAsync(db, Input.ServiceCategoryId, ct))
        {
            ModelState.AddModelError("Input.ServiceCategoryId", "Danh mục không hợp lệ.");
            return Page();
        }

        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Name) : SlugHelper.Generate(Input.Slug);
        if (await db.ServiceItems.AnyAsync(s => s.Slug == slug && s.Id != id && !s.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }

        BangGiaThemModel.MapToEntity(entity, Input, slug);
        await repo.UpdateAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật dịch vụ.");

        if (submitAction == "continue")
            return RedirectToPage(new { id });

        return RedirectToPage("/Admin/DichVu/BangGia/Index");
    }

    private async Task LoadCategoriesAsync(CancellationToken ct)
    {
        var cats = await db.ServiceCategories.AsNoTracking()
            .Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync(ct);
        CategoryOptions = new SelectList(cats, "Id", "Name", Input.ServiceCategoryId);
        ViewData["CategoryOptions"] = CategoryOptions;
        CategoryName = cats.FirstOrDefault(c => c.Id == Input.ServiceCategoryId)?.Name ?? CategoryName;
    }

    private static ServiceItemInputModel MapFromEntity(ServiceItem s) => new()
    {
        Id = s.Id,
        ServiceCategoryId = s.ServiceCategoryId,
        Name = s.Name,
        Slug = s.Slug,
        ShortDescription = s.ShortDescription,
        DetailDescription = s.DetailDescription,
        IncludesLines = string.Join(Environment.NewLine, ServiceItemJson.ParseIncludes(s.IncludesJson)),
        EstimatedPriceText = s.EstimatedPriceText,
        EstimatedDurationText = s.EstimatedDurationText,
        PriceNote = s.PriceNote,
        IconKey = s.IconKey,
        DisplayOrder = s.DisplayOrder,
        IsFeatured = s.IsFeatured,
        IsActive = s.IsActive,
        MetaTitle = s.MetaTitle,
        MetaDescription = s.MetaDescription,
        MetaKeywords = s.MetaKeywords,
        OgImageUrl = s.OgImageUrl,
        CanonicalUrl = s.CanonicalUrl
    };
}
