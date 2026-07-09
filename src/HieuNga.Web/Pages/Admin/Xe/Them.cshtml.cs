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

public class ThemModel(
    IRepository<Motorcycle> repository,
    IUnitOfWork uow,
    HieuNgaDbContext db,
    IImageStorageService imageStorage) : PageModel
{
    [BindProperty]
    public MotorcycleInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? ThumbnailFile { get; set; }

    public bool SupportsImageUpload => imageStorage.SupportsUpload;
    public string ImageStorageNote => imageStorage.StorageDescription;

    public SelectList CategoryOptions => new(
        Enum.GetValues<Domain.Enums.MotorcycleCategory>().Select(c => new { Value = (int)c, Text = c.ToString() }),
        "Value", "Text", (int)Input.Category);

    public void OnGet()
    {
        ViewData["Title"] = "Thêm xe mới";
        ViewData["CategoryOptions"] = CategoryOptions;
        ViewData["SupportsImageUpload"] = SupportsImageUpload;
        ViewData["ImageStorageNote"] = ImageStorageNote;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thêm xe mới";
        ViewData["CategoryOptions"] = CategoryOptions;
        ViewData["SupportsImageUpload"] = SupportsImageUpload;
        ViewData["ImageStorageNote"] = ImageStorageNote;
        if (!ModelState.IsValid) return Page();

        var uploadedUrl = await MotorcycleImageUploadHelper.TryUploadThumbnailAsync(
            ThumbnailFile, imageStorage, ModelState, cancellationToken: ct);
        if (!ModelState.IsValid) return Page();

        var slug = string.IsNullOrWhiteSpace(Input.Slug) ? SlugHelper.Generate(Input.Name) : SlugHelper.Generate(Input.Slug);
        if (await db.Motorcycles.AnyAsync(m => m.Slug == slug && !m.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            return Page();
        }

        var entity = new Motorcycle
        {
            Name = Input.Name.Trim(),
            Slug = slug,
            Category = Input.Category,
            BasePrice = Input.BasePrice,
            ShortDescription = Input.ShortDescription,
            Description = Input.Description,
            IsPublished = Input.IsPublished,
            IsFeatured = Input.IsFeatured,
            SortOrder = Input.SortOrder,
            ThumbnailUrl = uploadedUrl ?? Input.ThumbnailUrl,
            MetaTitle = Input.MetaTitle,
            MetaDescription = Input.MetaDescription,
            MetaKeywords = Input.MetaKeywords,
            OgImageUrl = Input.OgImageUrl,
            CanonicalUrl = Input.CanonicalUrl
        };

        await repository.AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm xe mới.");
        return RedirectToPage("./Sua", new { id = entity.Id });
    }
}
