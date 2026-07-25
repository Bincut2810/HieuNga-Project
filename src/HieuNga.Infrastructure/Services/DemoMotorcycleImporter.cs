using System.Text.Json;
using System.Text.RegularExpressions;
using HieuNga.Application.DemoImport;
using HieuNga.Application.Interfaces;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Services;

public sealed class DemoMotorcycleImporter(
    HieuNgaDbContext db,
    IImageStorageService imageStorage,
    IHostEnvironment environment,
    ILogger<DemoMotorcycleImporter> logger) : IDemoMotorcycleImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg"];

    public string AssetsRootPath => ResolveAssetsRoot();

    public bool StorageReady => imageStorage.SupportsUpload;

    public string StorageDescription => imageStorage.StorageDescription;

    public async Task<IReadOnlyList<DemoPackageInfo>> ListPackagesAsync(CancellationToken ct = default)
    {
        var root = AssetsRootPath;
        var slugs = new List<string>();
        var packages = new List<DemoPackageInfo>();

        foreach (var (id, display, folder) in DemoPackageCatalog.All)
        {
            var packageDir = Path.Combine(root, folder);
            var metaPath = Path.Combine(packageDir, "metadata.json");
            var hasMeta = File.Exists(metaPath);
            string? slug = null;
            string? preview = null;

            if (hasMeta)
            {
                try
                {
                    var meta = await ReadMetadataAsync(metaPath, ct);
                    slug = meta.Slug;
                    if (!string.IsNullOrWhiteSpace(slug))
                        slugs.Add(slug);
                    preview = FindLocalPreviewUrl(packageDir, meta);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed reading demo metadata for {Package}", folder);
                }
            }

            packages.Add(new DemoPackageInfo(
                id, display, folder, hasMeta, false, null, slug, preview,
                hasMeta ? "Sẵn sàng import" : "Chưa có metadata"));
        }

        var imported = await db.Motorcycles.AsNoTracking()
            .Where(m => !m.IsDeleted && slugs.Contains(m.Slug))
            .Select(m => new { m.Id, m.Slug, m.ThumbnailUrl })
            .ToListAsync(ct);

        return packages.Select(p =>
        {
            var row = imported.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(p.Slug) &&
                string.Equals(x.Slug, p.Slug, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                return p with
                {
                    IsImported = false,
                    StatusLabel = p.HasMetadata ? "Chưa import" : "Chưa có package"
                };
            }

            return p with
            {
                IsImported = true,
                MotorcycleId = row.Id,
                ThumbnailPreviewUrl = row.ThumbnailUrl ?? p.ThumbnailPreviewUrl,
                StatusLabel = "Đã import"
            };
        }).ToList();
    }

    public async Task<DemoImportResult> ImportAsync(string packageId, CancellationToken ct = default)
    {
        var catalog = DemoPackageCatalog.All.FirstOrDefault(p =>
            string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Folder, packageId, StringComparison.OrdinalIgnoreCase));

        if (catalog == default)
            return Fail($"Package '{packageId}' không hợp lệ.");

        var packageDir = Path.Combine(AssetsRootPath, catalog.Folder);
        var metaPath = Path.Combine(packageDir, "metadata.json");
        if (!File.Exists(metaPath))
            return Fail($"Thiếu metadata.json trong DemoAssets/{catalog.Folder}.");

        if (!imageStorage.SupportsUpload)
            return Fail("Upload ảnh chưa sẵn sàng. Cấu hình Cloudinary (Production) hoặc Local (Development).");

        DemoMotorcycleMetadata meta;
        try
        {
            meta = await ReadMetadataAsync(metaPath, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid metadata for {Package}", catalog.Folder);
            return Fail($"metadata.json không hợp lệ: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(meta.Name) || string.IsNullOrWhiteSpace(meta.Slug))
            return Fail("metadata.json cần có name và slug.");

        meta.Slug = meta.Slug.Trim().ToLowerInvariant();
        var warnings = new List<string>();
        var uploaded = 0;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var bike = await db.Motorcycles
                .Include(m => m.Variants)
                .Include(m => m.Colors)
                .Include(m => m.MediaAssets)
                .Include(m => m.Features)
                .Include(m => m.Technologies)
                .Include(m => m.SpinFrames)
                .FirstOrDefaultAsync(m => m.Slug == meta.Slug, ct);

            var isNew = bike is null;
            if (bike is null)
            {
                bike = new Motorcycle { Slug = meta.Slug };
                db.Motorcycles.Add(bike);
            }
            else
            {
                bike.IsDeleted = false;
                ClearChildren(bike);
            }

            ApplyScalarFields(bike, meta);

            // Persist early to get Id for upload folder paths when new
            await db.SaveChangesAsync(ct);
            var idFolder = bike.Id.ToString("N");

            var thumb = await UploadIfExistsAsync(
                packageDir, meta.Assets.Thumbnail, $"demo/{idFolder}/thumb", ct, warnings);
            if (thumb is not null)
            {
                bike.ThumbnailUrl = thumb;
                uploaded++;
            }
            else if (string.IsNullOrWhiteSpace(bike.ThumbnailUrl))
            {
                warnings.Add("Thiếu thumbnail — xe vẫn được tạo, hãy thêm ảnh sau.");
            }

            if (!string.IsNullOrWhiteSpace(bike.ThumbnailUrl) && string.IsNullOrWhiteSpace(bike.OgImageUrl))
                bike.OgImageUrl = bike.ThumbnailUrl;

            uploaded += await ImportGalleryAsync(bike, packageDir, meta, idFolder, ct, warnings);
            uploaded += await ImportColorsAsync(bike, packageDir, meta, idFolder, ct, warnings);
            uploaded += await ImportSpinAsync(bike, packageDir, meta, idFolder, ct, warnings);
            uploaded += await ImportFeaturesAsync(bike, packageDir, meta, idFolder, ct, warnings);
            uploaded += await ImportTechnologyAsync(bike, packageDir, meta, idFolder, ct, warnings);
            ImportVariants(bike, meta);

            await db.SaveChangesAsync(ct);
            await SaveFinancePrefsAsync(bike.Id, meta.Finance, ct);
            await tx.CommitAsync(ct);

            var action = isNew ? "Đã import" : "Đã cập nhật (reimport)";
            logger.LogInformation("Demo import {Action} {Slug} ({Uploads} images)", action, meta.Slug, uploaded);
            return new DemoImportResult(
                true,
                $"{action} package {catalog.DisplayName}.",
                bike.Id,
                bike.Slug,
                uploaded,
                warnings);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "Demo import failed for {Package}", catalog.Folder);
            return Fail($"Import thất bại: {ex.Message}");
        }
    }

    public async Task<DemoImportResult> DeleteDemoAsync(string packageId, CancellationToken ct = default)
    {
        var packages = await ListPackagesAsync(ct);
        var pkg = packages.FirstOrDefault(p =>
            string.Equals(p.PackageId, packageId, StringComparison.OrdinalIgnoreCase));

        if (pkg is null)
            return Fail("Package không tồn tại.");

        if (string.IsNullOrWhiteSpace(pkg.Slug))
            return Fail("Package chưa có slug trong metadata — không thể xóa.");

        var bike = await db.Motorcycles.FirstOrDefaultAsync(m => m.Slug == pkg.Slug && !m.IsDeleted, ct);
        if (bike is null)
            return Fail("Xe demo chưa được import.");

        bike.IsDeleted = true;
        bike.IsPublished = false;
        bike.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new DemoImportResult(true, $"Đã xóa demo '{bike.Name}' (soft delete).", bike.Id, bike.Slug, 0, []);
    }

    private string ResolveAssetsRoot()
    {
        var candidates = new[]
        {
            Path.Combine(environment.ContentRootPath, "DemoAssets"),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "docs", "DemoAssets")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "DemoAssets"))
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(path))
                return path;
        }

        var fallback = Path.Combine(environment.ContentRootPath, "DemoAssets");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static async Task<DemoMotorcycleMetadata> ReadMetadataAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var meta = await JsonSerializer.DeserializeAsync<DemoMotorcycleMetadata>(stream, JsonOptions, ct);
        return meta ?? throw new InvalidOperationException("Empty metadata.");
    }

    private static void ApplyScalarFields(Motorcycle bike, DemoMotorcycleMetadata meta)
    {
        bike.Name = meta.Name.Trim();
        bike.Slug = meta.Slug;
        bike.Category = DemoPackageCatalog.ParseCategory(meta.Category);
        bike.BasePrice = meta.Price;
        bike.IsFeatured = meta.Featured;
        bike.IsPublished = meta.Published;
        bike.SortOrder = meta.SortOrder;
        bike.ShortDescription = meta.ShortDescription;
        bike.Description = meta.DescriptionHtml;
        bike.EngineCc = meta.EngineCc;
        bike.FuelType = meta.FuelType;
        bike.Transmission = meta.Transmission;
        bike.HighlightsJson = JsonSerializer.Serialize(meta.Highlights ?? [], JsonOptions);
        bike.TechnicalSpecsJson = JsonSerializer.Serialize(
            (meta.Specifications ?? []).Select(s => new { icon = s.Icon, label = s.Label, value = s.Value }),
            JsonOptions);
        bike.MetaTitle = meta.Seo.MetaTitle ?? $"{meta.Name} | Xe Máy Hiếu Nga";
        bike.MetaDescription = meta.Seo.MetaDescription ?? meta.ShortDescription;
        bike.MetaKeywords = meta.Seo.MetaKeywords;
        bike.CanonicalUrl = meta.Seo.CanonicalUrl;
        bike.UpdatedAt = DateTime.UtcNow;
    }

    private void ClearChildren(Motorcycle bike)
    {
        if (bike.Variants.Count > 0)
        {
            db.MotorcycleVariants.RemoveRange(bike.Variants);
            bike.Variants.Clear();
        }
        if (bike.Colors.Count > 0)
        {
            db.MotorcycleColors.RemoveRange(bike.Colors);
            bike.Colors.Clear();
        }
        if (bike.MediaAssets.Count > 0)
        {
            db.MediaAssets.RemoveRange(bike.MediaAssets);
            bike.MediaAssets.Clear();
        }
        if (bike.Features.Count > 0)
        {
            db.MotorcycleFeatures.RemoveRange(bike.Features);
            bike.Features.Clear();
        }
        if (bike.Technologies.Count > 0)
        {
            db.MotorcycleTechnologies.RemoveRange(bike.Technologies);
            bike.Technologies.Clear();
        }
        if (bike.SpinFrames.Count > 0)
        {
            db.MotorcycleSpinFrames.RemoveRange(bike.SpinFrames);
            bike.SpinFrames.Clear();
        }
    }

    private void ImportVariants(Motorcycle bike, DemoMotorcycleMetadata meta)
    {
        var variants = meta.Variants;
        if (variants.Count == 0)
        {
            variants =
            [
                new DemoVariantItem
                {
                    Name = "Tiêu chuẩn",
                    Price = meta.Price,
                    StockQuantity = 5,
                    IsAvailable = true
                }
            ];
        }

        var i = 0;
        foreach (var v in variants)
        {
            bike.Variants.Add(new MotorcycleVariant
            {
                MotorcycleId = bike.Id,
                Name = string.IsNullOrWhiteSpace(v.Name) ? $"Phiên bản {++i}" : v.Name,
                Price = v.Price ?? meta.Price,
                StockQuantity = v.StockQuantity,
                IsAvailable = v.IsAvailable,
                Sku = v.Sku
            });
        }
    }

    private async Task<int> ImportGalleryAsync(
        Motorcycle bike, string packageDir, DemoMotorcycleMetadata meta, string idFolder,
        CancellationToken ct, List<string> warnings)
    {
        var dir = Path.Combine(packageDir, meta.Assets.GalleryFolder);
        var files = ListImageFiles(dir);
        var count = 0;
        var order = 0;
        foreach (var file in files)
        {
            var url = await UploadFileAsync(file, $"demo/{idFolder}/gallery", ct, warnings);
            if (url is null) continue;
            bike.MediaAssets.Add(new MediaAsset
            {
                MotorcycleId = bike.Id,
                FileName = Path.GetFileName(file),
                Url = url,
                AltText = bike.Name,
                Type = MediaType.Image,
                SortOrder = order++
            });
            count++;
        }
        if (files.Count == 0)
            warnings.Add("Thư mục gallery trống — dùng placeholder hoặc thêm ảnh sau.");
        return count;
    }

    private async Task<int> ImportColorsAsync(
        Motorcycle bike, string packageDir, DemoMotorcycleMetadata meta, string idFolder,
        CancellationToken ct, List<string> warnings)
    {
        var dir = Path.Combine(packageDir, meta.Assets.ColorsFolder);
        var count = 0;
        var order = 0;
        var colors = meta.Colors;
        if (colors.Count == 0)
        {
            colors =
            [
                new DemoColorItem { Name = "Đen", Hex = "#111111", Image = "black.jpg" },
                new DemoColorItem { Name = "Trắng", Hex = "#F5F5F5", Image = "white.jpg" },
                new DemoColorItem { Name = "Đỏ", Hex = "#E40521", Image = "red.jpg" }
            ];
        }

        foreach (var c in colors)
        {
            string? url = null;
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(c.Image))
                candidates.Add(c.Image);
            candidates.Add($"{Slugify(c.Name)}.jpg");
            candidates.Add($"{Slugify(c.Name)}.svg");

            foreach (var name in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var path = ResolveExistingFile(dir, name);
                if (path is null) continue;
                url = await UploadFileAsync(path, $"demo/{idFolder}/colors", ct, warnings);
                if (url is not null) { count++; break; }
            }

            bike.Colors.Add(new MotorcycleColor
            {
                MotorcycleId = bike.Id,
                Name = c.Name,
                HexCode = string.IsNullOrWhiteSpace(c.Hex) ? "#000000" : c.Hex,
                ImageUrl = url,
                SortOrder = order++
            });
        }

        return count;
    }

    private async Task<int> ImportSpinAsync(
        Motorcycle bike, string packageDir, DemoMotorcycleMetadata meta, string idFolder,
        CancellationToken ct, List<string> warnings)
    {
        var dir = Path.Combine(packageDir, meta.Assets.SpinFolder);
        var files = ListImageFiles(dir);
        var count = 0;
        foreach (var file in files)
        {
            var frame = TryParseFrameIndex(Path.GetFileNameWithoutExtension(file));
            var url = await UploadFileAsync(file, $"demo/{idFolder}/360", ct, warnings);
            if (url is null) continue;
            bike.SpinFrames.Add(new MotorcycleSpinFrame
            {
                MotorcycleId = bike.Id,
                ImageUrl = url,
                FrameIndex = frame ?? count
            });
            count++;
        }
        if (files.Count > 0 && files.Count < 2)
            warnings.Add("360 cần ≥ 2 khung hình để viewer hoạt động.");
        return count;
    }

    private async Task<int> ImportFeaturesAsync(
        Motorcycle bike, string packageDir, DemoMotorcycleMetadata meta, string idFolder,
        CancellationToken ct, List<string> warnings)
    {
        var dir = Path.Combine(packageDir, meta.Assets.FeaturesFolder);
        var count = 0;
        var order = 0;
        foreach (var item in meta.Features)
        {
            string? url = null;
            if (!string.IsNullOrWhiteSpace(item.Image))
            {
                var path = ResolveExistingFile(dir, item.Image) ?? ResolveExistingFile(packageDir, item.Image);
                if (path is not null)
                {
                    url = await UploadFileAsync(path, $"demo/{idFolder}/features", ct, warnings);
                    if (url is not null) count++;
                }
            }

            // Fallback: use thumbnail so public detail feature UI still has an image
            url ??= bike.ThumbnailUrl;

            bike.Features.Add(new MotorcycleFeature
            {
                MotorcycleId = bike.Id,
                Title = item.Title,
                Description = item.Description,
                ImageUrl = url ?? "",
                SortOrder = order++
            });
        }
        return count;
    }

    private async Task<int> ImportTechnologyAsync(
        Motorcycle bike, string packageDir, DemoMotorcycleMetadata meta, string idFolder,
        CancellationToken ct, List<string> warnings)
    {
        var dir = Path.Combine(packageDir, meta.Assets.TechnologyFolder);
        var count = 0;
        var order = 0;
        foreach (var item in meta.Technology)
        {
            string? url = null;
            if (!string.IsNullOrWhiteSpace(item.Image))
            {
                var path = ResolveExistingFile(dir, item.Image) ?? ResolveExistingFile(packageDir, item.Image);
                if (path is not null)
                {
                    url = await UploadFileAsync(path, $"demo/{idFolder}/technology", ct, warnings);
                    if (url is not null) count++;
                }
            }

            url ??= bike.ThumbnailUrl;

            bike.Technologies.Add(new MotorcycleTechnology
            {
                MotorcycleId = bike.Id,
                Title = item.Title,
                Description = item.Description,
                ImageUrl = url ?? "",
                SortOrder = order++
            });
        }
        return count;
    }

    private async Task SaveFinancePrefsAsync(Guid motorcycleId, DemoFinanceDefaults finance, CancellationToken ct)
    {
        var key = $"motorcycle.finance.{motorcycleId:N}";
        // PascalCase to match MotorcycleFinancePrefs.LoadAsync default deserializer
        var payload = JsonSerializer.Serialize(new
        {
            CalculatorEnabled = finance.CalculatorEnabled,
            DefaultBankId = finance.DefaultBankId,
            DefaultDownPaymentPercent = finance.DefaultDownPaymentPercent,
            DefaultTermMonths = finance.DefaultTermMonths
        });

        var row = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, ct);
        if (row is null)
        {
            db.SiteSettings.Add(new SiteSetting
            {
                Key = key,
                Value = payload,
                Group = "motorcycle-finance"
            });
        }
        else
        {
            row.Value = payload;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<string?> UploadIfExistsAsync(
        string packageDir, string relativeName, string folder, CancellationToken ct, List<string> warnings)
    {
        var path = ResolveExistingFile(packageDir, relativeName);
        if (path is null)
        {
            // try basename with alternate extensions
            var stem = Path.GetFileNameWithoutExtension(relativeName);
            path = ImageExtensions
                .Select(ext => Path.Combine(packageDir, stem + ext))
                .FirstOrDefault(File.Exists);
        }

        if (path is null) return null;
        return await UploadFileAsync(path, folder, ct, warnings);
    }

    private async Task<string?> UploadFileAsync(string path, string folder, CancellationToken ct, List<string> warnings)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var fileName = Path.GetFileName(path);
            var contentType = GuessContentType(fileName);
            var result = await imageStorage.UploadAsync(stream, fileName, contentType, folder, ct);
            if (!result.Success)
            {
                warnings.Add($"{fileName}: {result.ErrorMessage}");
                return null;
            }
            return result.PublicUrl;
        }
        catch (Exception ex)
        {
            warnings.Add($"{Path.GetFileName(path)}: {ex.Message}");
            return null;
        }
    }

    private static string? ResolveExistingFile(string dir, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var direct = Path.Combine(dir, fileName);
        if (File.Exists(direct)) return direct;

        var stem = Path.GetFileNameWithoutExtension(fileName);
        foreach (var ext in ImageExtensions)
        {
            var candidate = Path.Combine(dir, stem + ext);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static List<string> ListImageFiles(string dir)
    {
        if (!Directory.Exists(dir)) return [];
        return Directory.EnumerateFiles(dir)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "image/jpeg"
        };

    private static int? TryParseFrameIndex(string name)
    {
        var match = Regex.Match(name, @"(\d+)(?!.*\d)");
        if (!match.Success) return null;
        return int.TryParse(match.Groups[1].Value, out var n) ? n : null;
    }

    private static string Slugify(string value)
    {
        var s = value.Trim().ToLowerInvariant();
        s = s.Replace("đ", "d");
        s = Regex.Replace(s, @"[^a-z0-9]+", "-");
        return s.Trim('-');
    }

    private static string? FindLocalPreviewUrl(string packageDir, DemoMotorcycleMetadata meta)
    {
        var thumb = ResolveExistingFile(packageDir, meta.Assets.Thumbnail)
                    ?? ImageExtensions.Select(ext => Path.Combine(packageDir, "thumbnail" + ext)).FirstOrDefault(File.Exists);
        if (thumb is null) return null;
        // Served via admin static mapping /demo-assets/...
        var fileName = Path.GetFileName(thumb);
        var folder = new DirectoryInfo(packageDir).Name;
        return $"/demo-assets/{folder}/{fileName}";
    }

    private static DemoImportResult Fail(string message) =>
        new(false, message, null, null, 0, []);
}
