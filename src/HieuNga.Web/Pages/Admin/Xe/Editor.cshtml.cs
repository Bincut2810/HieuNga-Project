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
using Microsoft.Extensions.Logging;

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
    IMotorcycleMediaStudioService mediaStudio,
    IImageUploadService imageUploader,
    ILogger<EditorModel> logger) : PageModel
{
    private const int SlugCollisionMaxTries = 1000;

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
    public VariantFormInput VariantForm { get; set; } = new();

    [BindProperty]
    public FeatureInput NewFeature { get; set; } = new();

    [BindProperty]
    public TechInput NewTech { get; set; } = new();

    public IReadOnlyList<VariantRow> Variants { get; private set; } = [];
    public IReadOnlyList<MotorcycleColor> Colors { get; private set; } = [];
    public IReadOnlyList<MotorcycleFeature> Features { get; private set; } = [];
    public IReadOnlyList<MotorcycleTechnology> Technologies { get; private set; } = [];
    public IReadOnlyList<MotorcycleSpinFrame> SpinFrames { get; private set; } = [];

    public SelectList CategoryOptions => new(
        MotorcycleCategoryLabels.All.Select(c => new { Value = (int)c.Value, Text = c.Label }),
        "Value", "Text", (int)Input.Category);

    public record VariantRow(Guid Id, string Name, decimal Price, int StockQuantity, bool IsAvailable);

    public class VariantFormInput
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
                VariantForm = new VariantFormInput
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
        // SaveGeneral consumes only `Input.*` (+ ThumbnailFile / PublishStatus /
        // Tab / Id). Use the WHITELIST variant so cross-tab nested BindProperty
        // pollution is stripped regardless of the key shape ASP.NET Core emits.
        //
        // Why whitelist and not blacklist: the runtime binding pipeline emits
        // unprefixed nested keys (Key=Name carrying the VariantForm.Name
        // message, Key=Title carrying NewFeature.Title / NewTech.Title).
        // The blacklist-by-prefix helper cannot reach those, so we keep only
        // the exact top-level form values this handler reads.
        EditorModelStateIsolation.RemoveAllExcept(ModelState,
            "Input", "ThumbnailFile", "PublishStatus", "Tab", "Id");
        logger.LogInformation("SaveGeneral started. IsCreate={IsCreate} Id={Id} RequestId={RequestId}",
            IsCreate, Id, HttpContext.TraceIdentifier);
        if (!ModelState.IsValid)
        {
            LogModelStateErrors("SaveGeneral");
            if (!IsCreate) await LoadRelatedAsync(Id!.Value, ct);
            return Page();
        }

        try
        {
            var result = await SaveCoreAsync(ct, "general");
            logger.LogInformation("SaveGeneral completed. IsCreate={IsCreate} Id={Id} RequestId={RequestId}",
                IsCreate, Id, HttpContext.TraceIdentifier);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "SaveGeneral failed. IsCreate={IsCreate} Id={Id} RequestId={RequestId}",
                IsCreate, Id, HttpContext.TraceIdentifier);
            this.SetError("Đã xảy ra lỗi khi lưu xe. Vui lòng thử lại hoặc liên hệ quản trị viên.");
            if (!IsCreate && Id.HasValue)
            {
                try { await LoadRelatedAsync(Id.Value, ct); }
                catch (Exception loadEx)
                {
                    logger.LogWarning(loadEx, "Failed to reload related data after save error.");
                }
            }
            return Page();
        }
    }

    private void LogModelStateErrors(string stage)
    {
        foreach (var kvp in ModelState)
        {
            if (kvp.Value.Errors.Count == 0) continue;
            foreach (var err in kvp.Value.Errors)
            {
                logger.LogWarning("ModelState invalid at {Stage}. Key={Key} Error={Error} Exception={Exception}",
                    stage, kvp.Key, err.ErrorMessage, err.Exception?.Message);
            }
        }
    }

    public async Task<IActionResult> OnPostSaveSeoAsync(CancellationToken ct)
    {
        Tab = "seo";
        SetViewData();
        // SaveSeo consumes only `Input.MetaTitle/MetaDescription/MetaKeywords/
        // OgImageUrl/CanonicalUrl` — keep only the whitelist.
        EditorModelStateIsolation.RemoveAllExcept(ModelState,
            "Input", "PublishStatus", "Tab", "Id");
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
        // SavePublish consumes only `Input.IsPublished/IsFeatured/SortOrder` +
        // `PublishStatus` — keep the whitelist.
        EditorModelStateIsolation.RemoveAllExcept(ModelState,
            "Input", "PublishStatus", "Tab", "Id");
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
        // AddFeature consumes only `NewFeature.*` (+ imageFile).
        EditorModelStateIsolation.RemoveAllExcept(ModelState,
            "NewFeature", "PublishStatus", "Tab", "Id");
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();

        var url = await TryStudioUploadAsync(imageFile, "features", ct, Id);
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
            var url = await TryStudioUploadAsync(imageFile, "features", ct, id);
            if (url is not null) item.ImageUrl = url;
        }
        item.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật tính năng.");
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
        // AddTech consumes only `NewTech.*` (+ imageFile).
        EditorModelStateIsolation.RemoveAllExcept(ModelState,
            "NewTech", "PublishStatus", "Tab", "Id");
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();

        var url = await TryStudioUploadAsync(imageFile, "technology", ct, Id);
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
            var url = await TryStudioUploadAsync(imageFile, "technology", ct, id);
            if (url is not null) item.ImageUrl = url;
        }
        item.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật công nghệ.");
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

    /// <summary>
    /// Save Specifications tab. The Spec Builder UI posts the lines as
    /// <c>SpecsLines</c>; serialize them with the existing project format
    /// (see <see cref="SerializeSpecs"/>) and persist to
    /// <c>Motorcycle.TechnicalSpecsJson</c>.
    /// </summary>
    public async Task<IActionResult> OnPostSaveSpecsAsync(CancellationToken ct)
    {
        Tab = "specifications";
        SetViewData();

        // SaveSpecs reads only SpecsLines + Id + Tab. Use the whitelist
        // approach to drop any cross-tab BindProperty pollution regardless
        // of the emitted key shape.
        EditorModelStateIsolation.RemoveAllExcept(ModelState,
            "SpecsLines", "Tab", "Id", "PublishStatus");

        if (IsCreate)
            return RedirectToPage(new { tab = "general" });

        // Capture the user-submitted SpecsLines BEFORE LoadMotorcycleAsync
        // overwrites the instance property with the parsed DB value
        // (ParseSpecsToLines(TechnicalSpecsJson)).
        var submittedLines = SpecsLines;

        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        var bike = await motorcycleRepo.GetByIdAsync(Id.Value, ct);
        if (bike is null) return NotFound();

        var json = SerializeSpecs(submittedLines);
        if (string.IsNullOrWhiteSpace(json) && !string.IsNullOrWhiteSpace(submittedLines))
        {
            // Lines were provided but serialized to nothing (all-blank) — keep the
            // original string so the user doesn't lose data on accidental trim.
            bike.TechnicalSpecsJson = submittedLines;
        }
        else
        {
            bike.TechnicalSpecsJson = json;
        }

        await motorcycleRepo.UpdateAsync(bike, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã lưu thông số kỹ thuật.");
        return RedirectToPage(new { id = Id, tab = "specifications" });
    }

    /// <summary>
    /// Save (create or update) a variant from the Finance tab. The
    /// <c>VariantForm.Id</c> field decides which path is taken:
    ///   - null/empty → create a new variant for the current motorcycle.
    ///   - set        → update the matching variant's editable fields.
    /// </summary>
    public async Task<IActionResult> OnPostSaveVariantAsync(CancellationToken ct)
    {
        Tab = "finance";
        SetViewData();

        // SaveVariant reads VariantForm.* + Id + Tab. Whitelist-isolate so
        // unrelated nested BindProperty pollution cannot block the save.
        EditorModelStateIsolation.RemoveAllExcept(ModelState,
            "VariantForm", "Tab", "Id", "PublishStatus");

        if (IsCreate)
            return RedirectToPage(new { tab = "general" });

        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();

        var name = (VariantForm.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản.");
            return Page();
        }

        if (VariantForm.Id.HasValue && VariantForm.Id.Value != Guid.Empty)
        {
            var variantId = VariantForm.Id.Value;
            var existing = await variantRepo.GetByIdAsync(variantId, ct);
            if (existing is null || existing.IsDeleted || existing.MotorcycleId != Id!.Value)
                return NotFound();

            existing.Name = name;
            existing.Price = VariantForm.Price;
            existing.StockQuantity = VariantForm.StockQuantity;
            existing.IsAvailable = VariantForm.IsAvailable;
            // Slug stays stable on edit; consumers reference it on the public detail page.
            await variantRepo.UpdateAsync(existing, ct);
            await uow.SaveChangesAsync(ct);
            this.SetSuccess("Đã cập nhật phiên bản.");
            return RedirectToPage(new { id = Id, tab = "finance" });
        }

        // Create a new variant. Slug defaults to a slugified name; uniqueness
        // is scoped per-motorcycle by appending "-2", "-3", … if needed.
        var baseSlug = SlugHelper.Generate(name);
        var slug = baseSlug;
        var n = 2;
        while (await db.MotorcycleVariants.AnyAsync(v =>
            v.MotorcycleId == Id!.Value && v.Slug == slug && !v.IsDeleted, ct))
        {
            slug = $"{baseSlug}-{n++}";
        }

        var entity = new MotorcycleVariant
        {
            MotorcycleId = Id!.Value,
            Name = name,
            Slug = slug,
            Price = VariantForm.Price,
            StockQuantity = VariantForm.StockQuantity,
            IsAvailable = VariantForm.IsAvailable
        };
        await variantRepo.AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm phiên bản.");
        return RedirectToPage(new { id = Id, tab = "finance" });
    }

    /// <summary>
    /// Soft-delete a variant via the existing repository conventions
    /// (<c>IRepository&lt;T&gt;.SoftDeleteAsync</c> flips <c>IsDeleted</c>
    /// + updates <c>UpdatedAt</c>).
    /// </summary>
    public async Task<IActionResult> OnPostDeleteVariantAsync(Guid variantId, CancellationToken ct)
    {
        Tab = "finance";
        SetViewData();

        // Drop any nested BindProperty pollution. The handler reads only
        // Id / Tab / variantId from the form.
        EditorModelStateIsolation.RemoveAllExcept(ModelState,
            "VariantForm", "Tab", "Id", "PublishStatus");

        if (IsCreate)
            return RedirectToPage(new { tab = "general" });

        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        if (variantId == Guid.Empty) return NotFound();

        var existing = await variantRepo.GetByIdAsync(variantId, ct);
        if (existing is null || existing.IsDeleted || existing.MotorcycleId != Id!.Value)
            return NotFound();

        await variantRepo.SoftDeleteAsync(existing, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa phiên bản.");
        return RedirectToPage(new { id = Id, tab = "finance" });
    }

    private async Task<IActionResult> SaveCoreAsync(CancellationToken ct, string returnTab)
    {
        ApplyPublishStatusToInput();

        // During edit, scope the motorcycle-thumbnail storage folder to this bike.
        // During create, the thumbnail goes to a generic folder — the user will
        // re-upload from the Media tab once the bike has an id.
        var thumbContextId = IsCreate ? null : (Id.HasValue ? Id.Value : (Guid?)null);
        var uploadedUrl = await TryStudioUploadAsync(ThumbnailFile, "motorcycles", ct, thumbContextId);
        if (!ModelState.IsValid)
        {
            LogModelStateErrors("SaveCoreAsync.PreSlug");
            if (!IsCreate) await LoadRelatedAsync(Id!.Value, ct);
            return Page();
        }

        var slug = string.IsNullOrWhiteSpace(Input.Slug)
            ? SlugHelper.Generate(Input.Name)
            : SlugHelper.Generate(Input.Slug);

        logger.LogInformation(
            "SaveCoreAsync preparing. IsCreate={IsCreate} BaseSlug={Slug} Name={Name} HasThumbnail={HasThumbnail}",
            IsCreate, slug, Input.Name, !string.IsNullOrWhiteSpace(uploadedUrl ?? Input.ThumbnailUrl));

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
                ModelState.AddModelError("Input.ThumbnailUrl", "Tạo xe nhanh cần ảnh đại diện (kéo thả / chọn / dán ảnh).");
                return Page();
            }

            // Reuse the project's existing duplicate-slug retry convention
            // (counter-suffix `-2`, `-3`, …). Mirrors the pattern already used
            // by OnPostDuplicateMotorcycleAsync for the `-copy` suffix.
            var resolvedSlug = await ResolveUniqueSlugAsync(slug, ct);
            if (!string.Equals(resolvedSlug, slug, StringComparison.Ordinal))
            {
                logger.LogInformation(
                    "SaveCoreAsync slug collision. Original={Original} Resolved={Resolved}",
                    slug, resolvedSlug);
            }

            var entity = new Motorcycle
            {
                Name = Input.Name.Trim(),
                Slug = resolvedSlug,
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
            logger.LogInformation(
                "SaveCoreAsync sending SaveChanges. IsCreate=true Slug={Slug} Name={Name}",
                resolvedSlug, entity.Name);
            await uow.SaveChangesAsync(ct);
            logger.LogInformation(
                "SaveCoreAsync SaveChanges succeeded. NewMotorcycleId={NewId} Slug={Slug}",
                entity.Id, resolvedSlug);
            this.SetSuccess("Đã tạo Draft. Tiếp tục thêm ảnh đại diện, màu và góc xem.");
            return RedirectToPage(new { id = entity.Id, tab = "media" });
        }

        var id = Id!.Value;
        if (await db.Motorcycles.AnyAsync(m => m.Slug == slug && m.Id != id && !m.IsDeleted, ct))
        {
            // Edit-mode slug collision: surface the error via the validation span added in the form.
            // Keep behavior minimal — the user can rename the slug and retry.
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
        logger.LogInformation(
            "SaveCoreAsync SaveChanges succeeded. IsCreate=false MotorcycleId={Id} Slug={Slug}",
            bike.Id, bike.Slug);
        this.SetSuccess("Đã lưu thay đổi.");
        return RedirectToPage(new { id, tab = returnTab });
    }

    private async Task<string> ResolveUniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = Guid.NewGuid().ToString("N")[..8];

        var candidate = baseSlug;
        var n = 2;
        while (await db.Motorcycles.AnyAsync(m => m.Slug == candidate && !m.IsDeleted, ct))
        {
            if (n > SlugCollisionMaxTries)
            {
                // Fall back to a random suffix rather than loop forever.
                candidate = $"{baseSlug}-{Guid.NewGuid():N}";
                break;
            }
            candidate = $"{baseSlug}-{n}";
            n++;
        }
        return candidate;
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

    private async Task<string?> TryStudioUploadAsync(IFormFile? file, string folder, CancellationToken ct, Guid? contextId = null)
    {
        if (file is null || file.Length == 0) return null;
        // Map the legacy static-folder call site to the canonical upload pipeline.
        // Folder is unused now — the kind drives the storage location.
        var kind = folder switch
        {
            "motorcycles" => ImageUploadKinds.MotorcycleThumbnail,
            "features" => ImageUploadKinds.MotorcycleFeature,
            "technology" => ImageUploadKinds.MotorcycleTechnology,
            _ => ImageUploadKinds.MotorcycleThumbnail
        };

        var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;
        var payload = new ImageUploadPayload
        {
            Content = ms,
            FileName = file.FileName ?? "upload",
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType!,
            Length = file.Length
        };
        var response = await imageUploader.UploadAsync(payload, kind, contextId, ct);
        if (!response.Ok)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Không tải được ảnh.");
            return null;
        }
        return response.Url;
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
