using HieuNga.Application.Interfaces;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Services;
using HieuNga.Web.Pages.Admin.Extensions;
using HieuNga.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Web.Pages.Admin.Xe;

public class SuaModel(
    IRepository<Motorcycle> repository,
    IUnitOfWork uow,
    HieuNgaDbContext db,
    IImageStorageService imageStorage) : PageModel
{
    public Guid Id { get; set; }

    [BindProperty]
    public MotorcycleInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? ThumbnailFile { get; set; }

    public bool SupportsImageUpload => imageStorage.SupportsUpload;
    public string ImageStorageNote => imageStorage.StorageDescription;

    public SelectList CategoryOptions => new(
        Enum.GetValues<Domain.Enums.MotorcycleCategory>().Select(c => new { Value = (int)c, Text = c.ToString() }),
        "Value", "Text", (int)Input.Category);

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa xe";
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();

        Id = entity.Id;
        Input = Map(entity);
        ViewData["CategoryOptions"] = CategoryOptions;
        ViewData["SupportsImageUpload"] = SupportsImageUpload;
        ViewData["ImageStorageNote"] = ImageStorageNote;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Sửa xe";
        Id = id;
        ViewData["CategoryOptions"] = CategoryOptions;
        ViewData["SupportsImageUpload"] = SupportsImageUpload;
        ViewData["ImageStorageNote"] = ImageStorageNote;
        if (!ModelState.IsValid) return Page();

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted) return NotFound();

        var uploadedUrl = await MotorcycleImageUploadHelper.TryUploadThumbnailAsync(
            ThumbnailFile, imageStorage, ModelState, cancellationToken: ct);
        if (!ModelState.IsValid) return Page();

        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Name) : SlugHelper.Generate(Input.Slug);
        if (await db.Motorcycles.AnyAsync(m => m.Slug == slug && m.Id != id && !m.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }

        entity.Name = Input.Name.Trim();
        entity.Slug = slug;
        entity.Category = Input.Category;
        entity.BasePrice = Input.BasePrice;
        entity.ShortDescription = Input.ShortDescription;
        entity.Description = Input.Description;
        entity.IsPublished = Input.IsPublished;
        entity.IsFeatured = Input.IsFeatured;
        entity.SortOrder = Input.SortOrder;
        entity.ThumbnailUrl = uploadedUrl ?? Input.ThumbnailUrl;
        entity.MetaTitle = Input.MetaTitle;
        entity.MetaDescription = Input.MetaDescription;
        entity.MetaKeywords = Input.MetaKeywords;
        entity.OgImageUrl = Input.OgImageUrl;
        entity.CanonicalUrl = Input.CanonicalUrl;

        await repository.UpdateAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật xe.");
        return RedirectToPage();
    }

    private static MotorcycleInputModel Map(Motorcycle m) => new()
    {
        Name = m.Name,
        Slug = m.Slug,
        Category = m.Category,
        BasePrice = m.BasePrice,
        ShortDescription = m.ShortDescription,
        Description = m.Description,
        IsPublished = m.IsPublished,
        IsFeatured = m.IsFeatured,
        SortOrder = m.SortOrder,
        ThumbnailUrl = m.ThumbnailUrl,
        MetaTitle = m.MetaTitle,
        MetaDescription = m.MetaDescription,
        MetaKeywords = m.MetaKeywords,
        OgImageUrl = m.OgImageUrl,
        CanonicalUrl = m.CanonicalUrl
    };
}
