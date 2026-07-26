using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HieuNga.Application.Interfaces;
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
/// Replaces fragmented Sua / Gia / NoiDung editing UX. Create via optional id.
/// </summary>
public class EditorModel(
    IRepository<Motorcycle> motorcycleRepo,
    IRepository<MotorcycleVariant> variantRepo,
    IUnitOfWork uow,
    HieuNgaDbContext db,
    IImageStorageService imageStorage,
    IFinanceConfigService financeConfig) : PageModel
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

    [BindProperty]
    public MotorcycleFinancePrefs FinancePrefs { get; set; } = new();

    public IReadOnlyList<VariantRow> Variants { get; private set; } = [];
    public IReadOnlyList<MotorcycleColor> Colors { get; private set; } = [];
    public IReadOnlyList<MediaAsset> Gallery { get; private set; } = [];
    public IReadOnlyList<MotorcycleFeature> Features { get; private set; } = [];
    public IReadOnlyList<MotorcycleTechnology> Technologies { get; private set; } = [];
    public IReadOnlyList<MotorcycleSpinFrame> SpinFrames { get; private set; } = [];
    public IReadOnlyList<Application.DTOs.FinanceBankDto> FinanceBanks { get; private set; } = [];

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
        if (bike.IsPublished)
            await MotorcycleFinancePrefs.EnsureDefaultsAsync(db, bike.Id, ct);
        this.SetSuccess("Đã cập nhật trạng thái publish.");
        return RedirectToPage(new { id = Id, tab = "publish" });
    }

    public async Task<IActionResult> OnPostSaveThumbnailAsync(CancellationToken ct)
    {
        Tab = "media";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();

        var uploadedUrl = await MotorcycleImageUploadHelper.TryUploadAsync(
            ThumbnailFile, imageStorage, ModelState, $"motorcycles/{Id:N}", "ThumbnailFile", ct);
        if (!ModelState.IsValid) return Page();
        if (uploadedUrl is null && string.IsNullOrWhiteSpace(Input.ThumbnailUrl))
        {
            ModelState.AddModelError(string.Empty, "Chọn ảnh hoặc dán URL thumbnail.");
            return Page();
        }

        var bike = await motorcycleRepo.GetByIdAsync(Id.Value, ct);
        if (bike is null) return NotFound();
        bike.ThumbnailUrl = uploadedUrl ?? Input.ThumbnailUrl;
        await motorcycleRepo.UpdateAsync(bike, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã cập nhật thumbnail.");
        return RedirectToPage(new { id = Id, tab = "media" });
    }

    public async Task<IActionResult> OnPostRemoveThumbnailAsync(CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        var bike = await motorcycleRepo.GetByIdAsync(Id.Value, ct);
        if (bike is null) return NotFound();
        bike.ThumbnailUrl = null;
        await motorcycleRepo.UpdateAsync(bike, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa thumbnail.");
        return RedirectToPage(new { id = Id, tab = "media" });
    }

    public async Task<IActionResult> OnPostSaveSpecsAsync(CancellationToken ct)
    {
        Tab = "specifications";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();

        var bike = await motorcycleRepo.GetByIdAsync(Id.Value, ct);
        if (bike is null) return NotFound();
        bike.TechnicalSpecsJson = SerializeSpecs(SpecsLines);
        await motorcycleRepo.UpdateAsync(bike, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã lưu thông số kỹ thuật.");
        return RedirectToPage(new { id = Id, tab = "specifications" });
    }

    public async Task<IActionResult> OnPostSaveVariantAsync(CancellationToken ct)
    {
        Tab = "finance";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        if (!ModelState.IsValid) return Page();

        var id = Id.Value;
        if (VariantForm.Id.HasValue)
        {
            var variant = await variantRepo.GetByIdAsync(VariantForm.Id.Value, ct);
            if (variant is null || variant.IsDeleted || variant.MotorcycleId != id) return NotFound();
            variant.Name = VariantForm.Name.Trim();
            variant.Price = VariantForm.Price;
            variant.StockQuantity = VariantForm.StockQuantity;
            variant.IsAvailable = VariantForm.IsAvailable;
            await variantRepo.UpdateAsync(variant, ct);
        }
        else
        {
            await variantRepo.AddAsync(new MotorcycleVariant
            {
                MotorcycleId = id,
                Name = VariantForm.Name.Trim(),
                Price = VariantForm.Price,
                StockQuantity = VariantForm.StockQuantity,
                IsAvailable = VariantForm.IsAvailable
            }, ct);
        }

        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã lưu phiên bản giá.");
        return RedirectToPage(new { id, tab = "finance" });
    }

    public async Task<IActionResult> OnPostDeleteVariantAsync(Guid variantId, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var variant = await variantRepo.GetByIdAsync(variantId, ct);
        if (variant is null || variant.MotorcycleId != id) return NotFound();
        await variantRepo.SoftDeleteAsync(variant, ct);
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa phiên bản.");
        return RedirectToPage(new { id, tab = "finance" });
    }

    public async Task<IActionResult> OnPostAddGalleryAsync(List<IFormFile>? galleryFiles, CancellationToken ct)
    {
        Tab = "media";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        if (galleryFiles is null || galleryFiles.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Chọn ít nhất một ảnh gallery.");
            return Page();
        }

        var id = Id.Value;
        var maxSort = await db.MediaAssets.Where(m => m.MotorcycleId == id && !m.IsDeleted)
            .Select(m => (int?)m.SortOrder).MaxAsync(ct) ?? -1;
        var sort = maxSort + 1;
        var added = 0;
        foreach (var file in galleryFiles.Where(f => f.Length > 0))
        {
            var url = await MotorcycleImageUploadHelper.TryUploadAsync(
                file, imageStorage, ModelState, $"gallery/{id:N}", "galleryFiles", ct);
            if (url is null) continue;
            db.MediaAssets.Add(new MediaAsset
            {
                MotorcycleId = id,
                FileName = file.FileName,
                Url = url,
                Type = MediaType.Image,
                SortOrder = sort++,
                FileSizeBytes = file.Length
            });
            added++;
        }

        if (added == 0)
        {
            if (ModelState.IsValid)
                ModelState.AddModelError(string.Empty, "Không tải được ảnh nào.");
            return Page();
        }

        await uow.SaveChangesAsync(ct);
        this.SetSuccess($"Đã thêm {added} ảnh gallery.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostReplaceGalleryAsync(Guid mediaId, IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "media";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == mediaId && m.MotorcycleId == Id && !m.IsDeleted, ct);
        if (asset is null) return NotFound();
        var url = await MotorcycleImageUploadHelper.TryUploadAsync(
            imageFile, imageStorage, ModelState, $"gallery/{Id:N}", "imageFile", ct);
        if (url is null)
        {
            ModelState.AddModelError(string.Empty, "Chọn ảnh để thay thế.");
            return Page();
        }
        asset.Url = url;
        asset.FileName = imageFile!.FileName;
        asset.FileSizeBytes = imageFile.Length;
        asset.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thay ảnh gallery.");
        return RedirectToPage(new { id = Id, tab = "media" });
    }

    public async Task<IActionResult> OnPostUpdateGalleryCaptionAsync(Guid mediaId, string? caption, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == mediaId && m.MotorcycleId == id && !m.IsDeleted, ct);
        if (asset is null) return NotFound();
        asset.AltText = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
        asset.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã lưu caption.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostReorderGalleryAsync(string? orderIds, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var ids = ParseGuidList(orderIds);
        var assets = await db.MediaAssets.Where(m => m.MotorcycleId == id && !m.IsDeleted).ToListAsync(ct);
        for (var i = 0; i < ids.Count; i++)
        {
            var asset = assets.FirstOrDefault(a => a.Id == ids[i]);
            if (asset is null) continue;
            asset.SortOrder = i;
            asset.UpdatedAt = DateTime.UtcNow;
        }
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã sắp xếp gallery.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostBulkDeleteGalleryAsync(List<Guid>? mediaIds, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        if (mediaIds is null || mediaIds.Count == 0)
        {
            this.SetError("Chọn ít nhất một ảnh để xóa.");
            return RedirectToPage(new { id, tab = "media" });
        }
        var assets = await db.MediaAssets
            .Where(m => m.MotorcycleId == id && mediaIds.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync(ct);
        foreach (var asset in assets)
        {
            asset.IsDeleted = true;
            asset.UpdatedAt = DateTime.UtcNow;
        }
        await uow.SaveChangesAsync(ct);
        this.SetSuccess($"Đã xóa {assets.Count} ảnh gallery.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostDeleteGalleryAsync(Guid mediaId, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == mediaId && m.MotorcycleId == id, ct);
        if (asset is not null)
        {
            asset.IsDeleted = true;
            asset.UpdatedAt = DateTime.UtcNow;
            await uow.SaveChangesAsync(ct);
        }
        this.SetSuccess("Đã xóa ảnh gallery.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostAddColorAsync(IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "media";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        if (!ModelState.IsValid) return Page();

        var url = await MotorcycleImageUploadHelper.TryUploadAsync(
            imageFile, imageStorage, ModelState, $"colors/{Id:N}", "imageFile", ct);
        if (string.IsNullOrWhiteSpace(url))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng tải ảnh màu sắc.");
            return Page();
        }

        var maxSort = await db.MotorcycleColors.Where(c => c.MotorcycleId == Id && !c.IsDeleted)
            .Select(c => (int?)c.SortOrder).MaxAsync(ct) ?? -1;

        db.MotorcycleColors.Add(new MotorcycleColor
        {
            MotorcycleId = Id.Value,
            Name = NewColor.Name.Trim(),
            HexCode = NewColor.HexCode.Trim(),
            ImageUrl = url,
            SortOrder = NewColor.SortOrder != 0 ? NewColor.SortOrder : maxSort + 1
        });
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thêm màu.");
        return RedirectToPage(new { id = Id, tab = "media" });
    }

    public async Task<IActionResult> OnPostReplaceColorImageAsync(Guid colorId, IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "media";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        var color = await db.MotorcycleColors.FirstOrDefaultAsync(c => c.Id == colorId && c.MotorcycleId == Id && !c.IsDeleted, ct);
        if (color is null) return NotFound();
        var url = await MotorcycleImageUploadHelper.TryUploadAsync(
            imageFile, imageStorage, ModelState, $"colors/{Id:N}", "imageFile", ct);
        if (url is null)
        {
            ModelState.AddModelError(string.Empty, "Chọn ảnh để thay thế.");
            return Page();
        }
        color.ImageUrl = url;
        color.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã thay ảnh màu.");
        return RedirectToPage(new { id = Id, tab = "media" });
    }

    public async Task<IActionResult> OnPostReorderColorsAsync(string? orderIds, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var ids = ParseGuidList(orderIds);
        var colors = await db.MotorcycleColors.Where(c => c.MotorcycleId == id && !c.IsDeleted).ToListAsync(ct);
        for (var i = 0; i < ids.Count; i++)
        {
            var color = colors.FirstOrDefault(c => c.Id == ids[i]);
            if (color is null) continue;
            color.SortOrder = i;
            color.UpdatedAt = DateTime.UtcNow;
        }
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã sắp xếp màu.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostDeleteColorAsync(Guid colorId, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var color = await db.MotorcycleColors.FirstOrDefaultAsync(c => c.Id == colorId && c.MotorcycleId == id, ct);
        if (color is not null)
        {
            color.IsDeleted = true;
            color.UpdatedAt = DateTime.UtcNow;
            await uow.SaveChangesAsync(ct);
        }
        this.SetSuccess("Đã xóa màu.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostUploadSpinAsync(List<IFormFile>? spinFiles, CancellationToken ct)
    {
        Tab = "media";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        if (spinFiles is null || spinFiles.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Chọn ít nhất một ảnh 360.");
            return Page();
        }

        var id = Id.Value;
        var files = spinFiles.Where(f => f.Length > 0).ToList();
        var numbered = files
            .Select(f => (File: f, Num: MotorcycleImageUploadHelper.TryParseFrameNumber(f.FileName)))
            .ToList();
        var useNumbers = numbered.All(x => x.Num.HasValue);

        var existingMax = await db.MotorcycleSpinFrames
            .Where(f => f.MotorcycleId == id && !f.IsDeleted)
            .Select(f => (int?)f.FrameIndex).MaxAsync(ct) ?? -1;
        var next = existingMax + 1;
        var added = 0;

        IEnumerable<(IFormFile File, int Index)> ordered;
        if (useNumbers)
        {
            ordered = numbered
                .OrderBy(x => x.Num!.Value)
                .Select(x => (x.File, x.Num!.Value));
        }
        else
        {
            ordered = files
                .OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                .Select(f => (f, next++));
        }

        foreach (var (file, frameIndex) in ordered)
        {
            var url = await MotorcycleImageUploadHelper.TryUploadAsync(
                file, imageStorage, ModelState, $"360/{id:N}", "spinFiles", ct);
            if (url is null) continue;
            db.MotorcycleSpinFrames.Add(new MotorcycleSpinFrame
            {
                MotorcycleId = id,
                ImageUrl = url,
                FrameIndex = frameIndex
            });
            added++;
        }

        if (added == 0)
        {
            if (ModelState.IsValid)
                ModelState.AddModelError(string.Empty, "Không tải được khung 360 nào.");
            return Page();
        }

        await uow.SaveChangesAsync(ct);
        this.SetSuccess(useNumbers
            ? $"Đã tải {added} khung 360 (theo số trong tên file)."
            : $"Đã tải {added} khung 360.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostReorderSpinAsync(string? orderIds, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var ids = ParseGuidList(orderIds);
        var frames = await db.MotorcycleSpinFrames.Where(f => f.MotorcycleId == id && !f.IsDeleted).ToListAsync(ct);
        for (var i = 0; i < ids.Count; i++)
        {
            var frame = frames.FirstOrDefault(f => f.Id == ids[i]);
            if (frame is null) continue;
            frame.FrameIndex = i;
            frame.UpdatedAt = DateTime.UtcNow;
        }
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã sắp xếp khung 360.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostReplaceSpinFrameAsync(Guid frameId, IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "media";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        var frame = await db.MotorcycleSpinFrames.FirstOrDefaultAsync(f => f.Id == frameId && f.MotorcycleId == Id && !f.IsDeleted, ct);
        if (frame is null) return NotFound();
        var url = await MotorcycleImageUploadHelper.TryUploadAsync(
            imageFile, imageStorage, ModelState, $"360/{Id:N}", "imageFile", ct);
        if (url is null)
        {
            ModelState.AddModelError(string.Empty, "Chọn ảnh để thay khung.");
            return Page();
        }
        frame.ImageUrl = url;
        frame.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        this.SetSuccess($"Đã thay Frame {frame.FrameIndex + 1:D3}.");
        return RedirectToPage(new { id = Id, tab = "media" });
    }

    public async Task<IActionResult> OnPostDeleteSpinFrameAsync(Guid frameId, CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var frame = await db.MotorcycleSpinFrames.FirstOrDefaultAsync(f => f.Id == frameId && f.MotorcycleId == id, ct);
        if (frame is not null)
        {
            frame.IsDeleted = true;
            frame.UpdatedAt = DateTime.UtcNow;
            await uow.SaveChangesAsync(ct);
        }
        this.SetSuccess("Đã xóa khung 360.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostClearSpinAsync(CancellationToken ct)
    {
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        var id = Id!.Value;
        var frames = await db.MotorcycleSpinFrames.Where(f => f.MotorcycleId == id && !f.IsDeleted).ToListAsync(ct);
        foreach (var f in frames)
        {
            f.IsDeleted = true;
            f.UpdatedAt = DateTime.UtcNow;
        }
        await uow.SaveChangesAsync(ct);
        this.SetSuccess("Đã xóa toàn bộ khung 360.");
        return RedirectToPage(new { id, tab = "media" });
    }

    public async Task<IActionResult> OnPostAddFeatureAsync(IFormFile? imageFile, CancellationToken ct)
    {
        Tab = "features";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();

        var url = await MotorcycleImageUploadHelper.TryUploadAsync(
            imageFile, imageStorage, ModelState, "features", "imageFile", ct);
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
            var url = await MotorcycleImageUploadHelper.TryUploadAsync(imageFile, imageStorage, ModelState, "features", "imageFile", ct);
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

        var url = await MotorcycleImageUploadHelper.TryUploadAsync(
            imageFile, imageStorage, ModelState, "technology", "imageFile", ct);
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
            var url = await MotorcycleImageUploadHelper.TryUploadAsync(imageFile, imageStorage, ModelState, "technology", "imageFile", ct);
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

    public async Task<IActionResult> OnPostSaveFinancePrefsAsync(CancellationToken ct)
    {
        Tab = "finance";
        SetViewData();
        if (IsCreate) return RedirectToPage(new { tab = "general" });
        if (!await LoadMotorcycleAsync(Id!.Value, ct)) return NotFound();
        await MotorcycleFinancePrefs.SaveAsync(db, Id.Value, FinancePrefs, ct);
        this.SetSuccess("Đã lưu finance settings.");
        return RedirectToPage(new { id = Id, tab = "finance" });
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
                FrameIndex = s.FrameIndex
            });
        }
        await uow.SaveChangesAsync(ct);

        var prefs = await MotorcycleFinancePrefs.LoadAsync(db, sourceId, ct);
        await MotorcycleFinancePrefs.SaveAsync(db, clone.Id, prefs, ct);

        this.SetSuccess("Đã nhân bản xe (Draft).");
        return RedirectToPage(new { id = clone.Id, tab = "general" });
    }

    private async Task<IActionResult> SaveCoreAsync(CancellationToken ct, string returnTab)
    {
        ApplyPublishStatusToInput();

        var uploadedUrl = await MotorcycleImageUploadHelper.TryUploadThumbnailAsync(
            ThumbnailFile, imageStorage, ModelState, cancellationToken: ct);
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
            this.SetSuccess("Đã tạo Draft. Tiếp tục thêm gallery, màu và 360.");
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
        Input = Map(bike);
        PublishStatus = bike.IsPublished ? "published" : "draft";
        SpecsLines = ParseSpecsToLines(bike.TechnicalSpecsJson);
        await LoadRelatedAsync(id, ct);
        FinancePrefs = await MotorcycleFinancePrefs.LoadAsync(db, id, ct);
        FinanceBanks = await financeConfig.GetActiveBanksAsync(ct);
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
            .Where(s => s.MotorcycleId == id && !s.IsDeleted).OrderBy(s => s.FrameIndex).ToListAsync(ct);
    }

    private async Task<string?> UploadOrFailAsync(IFormFile? file, string folder, CancellationToken ct) =>
        await MotorcycleImageUploadHelper.TryUploadAsync(file, imageStorage, ModelState, folder, cancellationToken: ct);

    private static List<Guid> ParseGuidList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
    }

    public IReadOnlyList<int> MissingSpinFrames =>
        MotorcycleImageUploadHelper.FindMissingFrameIndices(SpinFrames.Select(f => f.FrameIndex));

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
