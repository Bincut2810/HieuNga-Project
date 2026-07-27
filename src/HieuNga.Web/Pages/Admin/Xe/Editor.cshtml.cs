using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Media;
using HieuNga.Domain;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
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

/// <summary>
/// Unified Motorcycle CMS editor (Sprint 2.1).
/// Media Studio is async via /admin/api/xe/{id}/media — this page hosts the shell.
/// </summary>
public class EditorModel(
    IRepository<Motorcycle> motorcycleRepo,
    IRepository<MotorcycleVariant> variantRepo,
    IUnitOfWork uow,
    HieuNgaDbContext db,
    IImageStorageService imageStorage,
    IMotorcycleMediaStudioService mediaStudio) : PageModel
{
    public static readonly string[] ValidTabs =
        ["general", "media", "specifications", "features", "finance", "seo", "publish"];

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "general";

    public bool IsCreate => Id is null || Id == Guid.Empty;
    public string MotorcycleName { get; private set; } = "Xe mới";
    public string? PublicSlug { get; private set; }
    public bool SupportsImageUpload => imageStorage.SupportsUpload;
    public string ImageStorageNote => imageStorage.StorageDescription;

    [BindProperty]
    public MotorcycleInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? ThumbnailFile { get; set; }

    [BindProperty]
    public string PublishStatus { get; set; } = "draft";

    [BindProperty]
    public string? SpecsLines { get; set; }

    [BindProperty]
    public VariantInput VariantForm { get; set; } = new();

    [BindProperty]
    public ColorInput NewColor { get; set; } = new();

    [BindProperty]
    public FeatureInput NewFeature { get; set; } = new();

    [BindProperty]
    public TechInput NewTech { get; set; } = new();

    public IReadOnlyList<VariantRow> Variants { get; private set; } = [];
    public IReadOnlyList<MotorcycleColor> Colors { get; private set; } = [];
    public IReadOnlyList<MediaAsset> Gallery { get; private set; } = [];
    public IReadOnlyList<MotorcycleFeature> Features { get; private set; } = [];
    public IReadOnlyList<MotorcycleTechnology> Technologies { get; private set; } = [];
    public IReadOnlyList<MotorcycleSpinFrame> SpinFrames { get; private set; } = [];

    /// <summary>CMS Media Studio hero URL (not editable on the general form).</summary>
    public string? HeroImageUrl { get; private set; }

    public SelectList CategoryOptions => new(
        MotorcycleCategoryLabels.All.Select(c => new { Value = (int)c.Value, Text = c.Label }),
        "Value", "Text", (int)Input.Category);

    public record VariantRow(Guid Id, string Name, decimal Price, int StockQuantity, bool IsAvailable);

    public class VariantInput
    {
        public Guid? Id { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên phiên bản")]
        public string Name { get; set; } = string.Empty;
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class ColorInput
    {
        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(20)]
        public string HexCode { get; set; } = "#000000";
        public int SortOrder { get; set; }
    }

    public class FeatureInput
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
    }

    public class TechInput
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid? edit, CancellationToken ct)
    {
        NormalizeTab();
        SetViewData();
        if (IsCreate)
        {
            ViewData["Title"] = "Thêm xe";
            PublishStatus = "draft";
            Input.IsPublished = false;
            return Page();
        }

        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        if (Tab == "finance" && edit.HasValue)
        {
            var v = Variants.FirstOrDefault(x => x.Id == edit.Value);
            if (v is not null)
                VariantForm = new VariantInput
                {
                    Id = v.Id,
                    Name = v.Name,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity,
                    IsAvailable = v.IsAvailable
                };
        }

        ViewData["Title"] = $"Sửa · {MotorcycleName}";
        return Page();
    }

    public async Task<IActionResult> OnPostSaveGeneralAsync(CancellationToken ct)
    {
        Tab = "general";
        SetViewData();
        if (!ModelState.IsValid)
        {
            if (!IsCreate) await LoadRelatedAsync(Id!.Value, ct);
            return Page();
        }

        return await SaveCoreAsync(ct, "general");
    }

    public async Task<IActionResult> OnPostSaveSeoAsync(CancellationToken ct)
    {
        Tab = "seo";
        SetViewData();
        if (IsCreate)
            return RedirectToPage(new { tab = "general" });

        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        var bike = await motorcycleRepo.GetByIdAsync(Id.Value, ct);
        if (bike is null) return NotFound();

        bike.MetaTitle = Input.MetaTitle;
        bike.MetaDescription = Input.MetaDescription;
        bike.MetaKeywords = Input.MetaKeywords;
        bike.OgImageUrl = Input.OgImageUrl;
        bike.CanonicalUrl = Input.CanonicalUrl;
        await motorcycleRepo.UpdateAsync(bike, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã lưu SEO.");
        return RedirectToPage(new { id = Id, tab = "seo" });
    }

    public async Task<IActionResult> OnPostSavePublishAsync(CancellationToken ct)
    {
        Tab = "publish";
        SetViewData();
        ApplyPublishStatusToInput();
        if (IsCreate)
            return RedirectToPage(new { tab = "general" });

        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        var bike = await motorcycleRepo.GetByIdAsync(Id.Value, ct);
        if (bike is null) return NotFound();

        bike.IsPublished = Input.IsPublished;
        bike.IsFeatured = Input.IsFeatured;
        bike.SortOrder = Input.SortOrder;
        await motorcycleRepo.UpdateAsync(bike, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật trạng thái publish.");
        return RedirectToPage(new { id = Id, tab = "publish" });
    }

    public async Task<IActionResult> OnPostAddFeatureAsync(IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "features";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();

        var url = await TryStudioUploadAsync(imageFile, "features", ct);
        if (string.IsNullOrWhiteSpace(url))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng tải ảnh điểm nổi bật.");
            return Page();
        }

        var maxSort = await db.MotorcycleFeatures.Where(f => f.MotorcycleId == Id && !f.IsDeleted)
            .Select(f => (int?)f.SortOrder).MaxAsync(ct) ?? -1;

        db.MotorcycleFeatures.Add(new MotorcycleFeature
        {
            MotorcycleId = Id.Value,
            Title = NewFeature.Title.Trim(),
            Description = NewFeature.Description?.Trim(),
            ImageUrl = url,
            SortOrder = NewFeature.SortOrder != 0 ? NewFeature.SortOrder : maxSort + 1
        });
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm điểm nổi bật.");
        return RedirectToPage(new { id = Id, tab = "features" });
    }

    public async Task<IActionResult> OnPostUpdateFeatureAsync(Guid itemId, string title, string? description, IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "features";
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var item = await db.MotorcycleFeatures.FirstOrDefaultAsync(f => f.Id == itemId && f.MotorcycleId == id && !f.IsDeleted, ct);
        if (item is null) return NotFound();
        item.Title = (title ?? "").Trim();
        item.Description = description?.Trim();
        if (imageFile is { Length: > 0 })
        {
            var url = await TryStudioUploadAsync(imageFile, "features", ct);
            if (url is not null) item.ImageUrl = url;
        }
        item.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật feature.");
        return RedirectToPage(new { id, tab = "features" });
    }

    public async Task<IActionResult> OnPostDuplicateFeatureAsync(Guid itemId, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var item = await db.MotorcycleFeatures.AsNoTracking().FirstOrDefaultAsync(f => f.Id == itemId && f.MotorcycleId == id && !f.IsDeleted, ct);
        if (item is null) return NotFound();
        var maxSort = await db.MotorcycleFeatures.Where(f => f.MotorcycleId == id && !f.IsDeleted).Select(f => (int?)f.SortOrder).MaxAsync(ct) ?? -1;
        db.MotorcycleFeatures.Add(new MotorcycleFeature
        {
            MotorcycleId = id,
            Title = item.Title + " (copy)",
            Description = item.Description,
            ImageUrl = item.ImageUrl,
            SortOrder = maxSort + 1
        });
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã nhân bản feature.");
        return RedirectToPage(new { id, tab = "features" });
    }

    public async Task<IActionResult> OnPostReorderFeaturesAsync(string? orderIds, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var ids = ParseGuidList(orderIds);
        var items = await db.MotorcycleFeatures.Where(f => f.MotorcycleId == id && !f.IsDeleted).ToListAsync(ct);
        for (var i = 0; i < ids.Count; i++)
        {
            var item = items.FirstOrDefault(x => x.Id == ids[i]);
            if (item is null) continue;
            item.SortOrder = i;
            item.UpdatedAt = DateTime.UtcNow;
        }
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã sắp xếp features.");
        return RedirectToPage(new { id, tab = "features" });
    }

    public async Task<IActionResult> OnPostDeleteFeatureAsync(Guid featureId, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var item = await db.MotorcycleFeatures.FirstOrDefaultAsync(f => f.Id == featureId && f.MotorcycleId == id, ct);
        if (item is not null)
        {
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
            await uow.SaveChangesAsync(ct);
        }
        this.SetSuccess("Đã xóa điểm nổi bật.");
        return RedirectToPage(new { id, tab = "features" });
    }

    public async Task<IActionResult> OnPostAddTechAsync(IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "features";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();

        var url = await TryStudioUploadAsync(imageFile, "technology", ct);
        if (string.IsNullOrWhiteSpace(url))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng tải ảnh công nghệ.");
            return Page();
        }

        var maxSort = await db.MotorcycleTechnologies.Where(t => t.MotorcycleId == Id && !t.IsDeleted)
            .Select(t => (int?)t.SortOrder).MaxAsync(ct) ?? -1;

        db.MotorcycleTechnologies.Add(new MotorcycleTechnology
        {
            MotorcycleId = Id.Value,
            Title = NewTech.Title.Trim(),
            Description = NewTech.Description?.Trim(),
            ImageUrl = url,
            SortOrder = NewTech.SortOrder != 0 ? NewTech.SortOrder : maxSort + 1
        });
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm công nghệ.");
        return RedirectToPage(new { id = Id, tab = "features" });
    }

    public async Task<IActionResult> OnPostUpdateTechAsync(Guid itemId, string title, string? description, IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "features";
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var item = await db.MotorcycleTechnologies.FirstOrDefaultAsync(t => t.Id == itemId && t.MotorcycleId == id && !t.IsDeleted, ct);
        if (item is null) return NotFound();
        item.Title = (title ?? "").Trim();
        item.Description = description?.Trim();
        if (imageFile is { Length: > 0 })
        {
            var url = await TryStudioUploadAsync(imageFile, "technology", ct);
            if (url is not null) item.ImageUrl = url;
        }
        item.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật technology.");
        return RedirectToPage(new { id, tab = "features" });
    }

    public async Task<IActionResult> OnPostDuplicateTechAsync(Guid itemId, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var item = await db.MotorcycleTechnologies.AsNoTracking().FirstOrDefaultAsync(t => t.Id == itemId && t.MotorcycleId == id && !t.IsDeleted, ct);
        if (item is null) return NotFound();
        var maxSort = await db.MotorcycleTechnologies.Where(t => t.MotorcycleId == id && !t.IsDeleted).Select(t => (int?)t.SortOrder).MaxAsync(ct) ?? -1;
        db.MotorcycleTechnologies.Add(new MotorcycleTechnology
        {
            MotorcycleId = id,
            Title = item.Title + " (copy)",
            Description = item.Description,
            ImageUrl = item.ImageUrl,
            SortOrder = maxSort + 1
        });
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã nhân bản technology.");
        return RedirectToPage(new { id, tab = "features" });
    }

    public async Task<IActionResult> OnPostReorderTechAsync(string? orderIds, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var ids = ParseGuidList(orderIds);
        var items = await db.MotorcycleTechnologies.Where(t => t.MotorcycleId == id && !t.IsDeleted).ToListAsync(ct);
        for (var i = 0; i < ids.Count; i++)
        {
            var item = items.FirstOrDefault(x => x.Id == ids[i]);
            if (item is null) continue;
            item.SortOrder = i;
            item.UpdatedAt = DateTime.UtcNow;
        }
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã sắp xếp technology.");
        return RedirectToPage(new { id, tab = "features" });
    }

    public async Task<IActionResult> OnPostDeleteTechAsync(Guid techId, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var item = await db.MotorcycleTechnologies.FirstOrDefaultAsync(t => t.Id == techId && t.MotorcycleId == id, ct);
        if (item is not null)
        {
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
            await uow.SaveChangesAsync(ct);
        }
        this.SetSuccess("Đã xóa công nghệ.");
        return RedirectToPage(new { id, tab = "features" });
    }

    public async Task<IActionResult> OnPostDuplicateMotorcycleAsync(CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var sourceId = Id!.Value;
        var source = await db.Motorcycles
            .Include(m => m.Variants)
            .Include(m => m.Colors)
            .Include(m => m.MediaAssets)
            .Include(m => m.Features)
            .Include(m => m.Technologies)
            .Include(m => m.SpinFrames)
            .FirstOrDefaultAsync(m => m.Id == sourceId && !m.IsDeleted, ct);
        if (source is null) return NotFound();

        var baseSlug = source.Slug + "-copy";
        var slug = baseSlug;
        var n = 2;
        while (await db.Motorcycles.AnyAsync(m => m.Slug == slug && !m.IsDeleted, ct))
            slug = $"{baseSlug}-{n++}";

        var clone = new Motorcycle
        {
            Name = source.Name + " (copy)",
            Slug = slug,
            ShortDescription = source.ShortDescription,
            Description = source.Description,
            Category = source.Category,
            BasePrice = source.BasePrice,
            EngineCc = source.EngineCc,
            FuelType = source.FuelType,
            Transmission = source.Transmission,
            HighlightsJson = source.HighlightsJson,
            TechnicalSpecsJson = source.TechnicalSpecsJson,
            IsFeatured = false,
            IsPublished = false,
            SortOrder = source.SortOrder,
            ThumbnailUrl = source.ThumbnailUrl,
            MetaTitle = source.MetaTitle,
            MetaDescription = source.MetaDescription,
            MetaKeywords = source.MetaKeywords,
            OgImageUrl = source.OgImageUrl,
            CanonicalUrl = null
        };
        await motorcycleRepo.AddAsync(clone, ct);
        await uow.SaveChangesAsync(ct);

        foreach (var v in source.Variants.Where(x => !x.IsDeleted))
        {
            db.MotorcycleVariants.Add(new MotorcycleVariant
            {
                MotorcycleId = clone.Id,
                Name = v.Name,
                Slug = v.Slug,
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                Sku = v.Sku,
                IsAvailable = v.IsAvailable
            });
        }
        foreach (var c in source.Colors.Where(x => !x.IsDeleted))
        {
            db.MotorcycleColors.Add(new MotorcycleColor
            {
                MotorcycleId = clone.Id,
                Name = c.Name,
                HexCode = c.HexCode,
                ImageUrl = c.ImageUrl,
                SortOrder = c.SortOrder
            });
        }
        foreach (var g in source.MediaAssets.Where(x => !x.IsDeleted))
        {
            db.MediaAssets.Add(new MediaAsset
            {
                MotorcycleId = clone.Id,
                FileName = g.FileName,
                Url = g.Url,
                AltText = g.AltText,
                Type = g.Type,
                FileSizeBytes = g.FileSizeBytes,
                SortOrder = g.SortOrder
            });
        }
        foreach (var f in source.Features.Where(x => !x.IsDeleted))
        {
            db.MotorcycleFeatures.Add(new MotorcycleFeature
            {
                MotorcycleId = clone.Id,
                Title = f.Title,
                Description = f.Description,
                ImageUrl = f.ImageUrl,
                SortOrder = f.SortOrder
            });
        }
        foreach (var t in source.Technologies.Where(x => !x.IsDeleted))
        {
            db.MotorcycleTechnologies.Add(new MotorcycleTechnology
            {
                MotorcycleId = clone.Id,
                Title = t.Title,
                Description = t.Description,
                ImageUrl = t.ImageUrl,
                SortOrder = t.SortOrder
            });
        }
        foreach (var s in source.SpinFrames.Where(x => !x.IsDeleted))
        {
            db.MotorcycleSpinFrames.Add(new MotorcycleSpinFrame
            {
                MotorcycleId = clone.Id,
                ImageUrl = s.ImageUrl,
                Angle = s.Angle
            });
        }
        await uow.SaveChangesAsync(ct);

        this.SetSuccess("Đã nhân bản xe (Draft).");
        return RedirectToPage(new { id = clone.Id, tab = "general" });
    }

    private async Task<IActionResult> SaveCoreAsync(CancellationToken ct, string returnTab)
    {
        ApplyPublishStatusToInput();

        var uploadedUrl = await TryStudioUploadAsync(ThumbnailFile, "motorcycles", ct);
        if (!ModelState.IsValid)
        {
            if (!IsCreate) await LoadRelatedAsync(Id!.Value, ct);
            return Page();
        }

        var slug = string.IsNullOrWhiteSpace(Input.Slug)
            ? SlugHelper.Generate(Input.Name)
            : SlugHelper.Generate(Input.Slug);

        if (IsCreate)
        {
            Input.IsPublished = false;
            PublishStatus = "draft";

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ModelState.AddModelError("Input.Name", "Vui lòng nhập tên xe.");
                return Page();
            }

            var thumb = uploadedUrl ?? Input.ThumbnailUrl;
            if (string.IsNullOrWhiteSpace(thumb))
            {
                ModelState.AddModelError("ThumbnailFile", "Quick Create cần thumbnail (kéo thả / chọn / dán ảnh).");
                return Page();
            }

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
                IsPublished = false,
                IsFeatured = false,
                SortOrder = Input.SortOrder,
                ThumbnailUrl = thumb,
                MetaTitle = Input.MetaTitle,
                MetaDescription = Input.MetaDescription,
                MetaKeywords = Input.MetaKeywords,
                OgImageUrl = Input.OgImageUrl,
                CanonicalUrl = Input.CanonicalUrl
            };
            await motorcycleRepo.AddAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
            this.SetSuccess("Đã tạo Draft. Tiếp tục thêm gallery, màu và góc xem.");
            return RedirectToPage(new { id = entity.Id, tab = "media" });
        }

        var id = Id!.Value;
        if (await db.Motorcycles.AnyAsync(m => m.Slug == slug && m.Id != id && !m.IsDeleted, ct))
        {
            ModelState.AddModelError("Input.Slug", "Slug đã tồn tại.");
            await LoadRelatedAsync(id, ct);
            return Page();
        }

        var bike = await motorcycleRepo.GetByIdAsync(id, ct);
        if (bike is null || bike.IsDeleted) return NotFound();

        bike.Name = Input.Name.Trim();
        bike.Slug = slug;
        bike.Category = Input.Category;
        bike.BasePrice = Input.BasePrice;
        bike.ShortDescription = Input.ShortDescription;
        bike.Description = Input.Description;
        bike.IsPublished = Input.IsPublished;
        bike.IsFeatured = Input.IsFeatured;
        bike.SortOrder = Input.SortOrder;
        if (uploadedUrl is not null || !string.IsNullOrWhiteSpace(Input.ThumbnailUrl))
            bike.ThumbnailUrl = uploadedUrl ?? Input.ThumbnailUrl;
        bike.MetaTitle = Input.MetaTitle;
        bike.MetaDescription = Input.MetaDescription;
        bike.MetaKeywords = Input.MetaKeywords;
        bike.OgImageUrl = Input.OgImageUrl;
        bike.CanonicalUrl = Input.CanonicalUrl;

        await motorcycleRepo.UpdateAsync(bike, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã lưu thay đổi.");
        return RedirectToPage(new { id, tab = returnTab });
    }

    private void ApplyPublishStatusToInput()
    {
        switch ((PublishStatus ?? "draft").ToLowerInvariant())
        {
            case "published":
                Input.IsPublished = true;
                break;
            case "archived":
                Input.IsPublished = false;
                Input.IsFeatured = false;
                break;
            default:
                Input.IsPublished = false;
                break;
        }
    }

    private void NormalizeTab()
    {
        Tab = (Tab ?? "general").Trim().ToLowerInvariant();
        if (!ValidTabs.Contains(Tab)) Tab = "general";
        if (IsCreate && Tab is not ("general" or "seo" or "publish"))
            Tab = "general";
    }

    private void SetViewData()
    {
        ViewData["CategoryOptions"] = CategoryOptions;
        ViewData["SupportsImageUpload"] = SupportsImageUpload;
        ViewData["ImageStorageNote"] = ImageStorageNote;
        ViewData["Title"] = IsCreate ? "Thêm xe" : $"Sửa · {MotorcycleName}";
    }

    private async Task<bool> LoadMotorcycleAsync(Guid id, CancellationToken ct)
    {
        var bike = await db.Motorcycles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
        if (bike is null) return false;

        Id = bike.Id;
        MotorcycleName = bike.Name;
        PublicSlug = bike.Slug;
        HeroImageUrl = bike.HeroImageUrl;
        Input = Map(bike);
        PublishStatus = bike.IsPublished ? "published" : "draft";
        SpecsLines = ParseSpecsToLines(bike.TechnicalSpecsJson);
        await LoadRelatedAsync(id, ct);
        return true;
    }

    private async Task LoadRelatedAsync(Guid id, CancellationToken ct)
    {
        Variants = await db.MotorcycleVariants.AsNoTracking()
            .Where(v => v.MotorcycleId == id && !v.IsDeleted)
            .OrderBy(v => v.Name)
            .Select(v => new VariantRow(v.Id, v.Name, v.Price, v.StockQuantity, v.IsAvailable))
            .ToListAsync(ct);
        Colors = await db.MotorcycleColors.AsNoTracking()
            .Where(c => c.MotorcycleId == id && !c.IsDeleted).OrderBy(c => c.SortOrder).ToListAsync(ct);
        Gallery = await db.MediaAssets.AsNoTracking()
            .Where(m => m.MotorcycleId == id && !m.IsDeleted && m.Type == MediaType.Image)
            .OrderBy(m => m.SortOrder).ToListAsync(ct);
        Features = await db.MotorcycleFeatures.AsNoTracking()
            .Where(f => f.MotorcycleId == id && !f.IsDeleted).OrderBy(f => f.SortOrder).ToListAsync(ct);
        Technologies = await db.MotorcycleTechnologies.AsNoTracking()
            .Where(t => t.MotorcycleId == id && !t.IsDeleted).OrderBy(t => t.SortOrder).ToListAsync(ct);
        SpinFrames = await db.MotorcycleSpinFrames.AsNoTracking()
            .Where(s => s.MotorcycleId == id && !s.IsDeleted).OrderBy(s => s.Angle).ToListAsync(ct);
    }


    private static List<Guid> ParseGuidList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
    }

    private async Task<string?> TryStudioUploadAsync(IFormFile? file, string folder, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return null;
        var upload = await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct);
        var (ok, url, error) = await mediaStudio.UploadOnlyAsync(upload, folder, ct);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Không tải được ảnh.");
            return null;
        }
        return url;
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

    private static string? ParseSpecsToLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var items = JsonSerializer.Deserialize<List<SpecJson>>(json);
            if (items is null) return null;
            return string.Join(Environment.NewLine, items.Select(s =>
                string.Equals(s.Icon, "group", StringComparison.OrdinalIgnoreCase)
                    ? $"## {s.Label}"
                    : $"{s.Label}|{s.Value}"));
        }
        catch { return json; }
    }

    private static string? SerializeSpecs(string? lines)
    {
        if (string.IsNullOrWhiteSpace(lines)) return null;
        var items = new List<SpecJson>();
        foreach (var raw in lines.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw.StartsWith("##"))
            {
                items.Add(new SpecJson { Icon = "group", Label = raw.TrimStart('#').Trim(), Value = "" });
                continue;
            }
            var parts = raw.Split('|', 2, StringSplitOptions.TrimEntries);
            items.Add(new SpecJson { Icon = "•", Label = parts[0], Value = parts.Length > 1 ? parts[1] : "" });
        }
        return items.Count == 0 ? null : JsonSerializer.Serialize(items);
    }

    private sealed class SpecJson
    {
        public string? Icon { get; set; }
        public string? Label { get; set; }
        public string? Value { get; set; }
    }
}
