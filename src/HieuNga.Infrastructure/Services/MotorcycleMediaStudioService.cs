using System.Text.RegularExpressions;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Media;
using HieuNga.Application.Options;
using HieuNga.Domain;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HieuNga.Infrastructure.Services;

public sealed class MotorcycleMediaStudioService(
    HieuNgaDbContext db,
    IImageStorageService storage,
    IOptions<ImageStorageOptions> options) : IMotorcycleMediaStudioService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif", "image/svg+xml"
    };

    public async Task<MediaStudioStateDto?> GetStateAsync(Guid motorcycleId, CancellationToken ct = default)
    {
        var bike = await LoadBikeAsync(motorcycleId, ct);
        return bike is null ? null : BuildState(bike);
    }

    public async Task<MediaMutationResult> SetSlotAsync(Guid motorcycleId, MediaSlot slot, MediaFileUpload file, CancellationToken ct = default)
    {
        if (slot != MediaSlot.Thumbnail)
            return Fail("Slot không hợp lệ.");

        var bike = await db.Motorcycles.FirstOrDefaultAsync(m => m.Id == motorcycleId && !m.IsDeleted, ct);
        if (bike is null) return Fail("Không tìm thấy xe.");

        var uploaded = await UploadValidatedAsync(file, Folder(motorcycleId, MediaSlot.Thumbnail), ct);
        if (!uploaded.Ok) return Fail(uploaded.Error!);

        bike.ThumbnailUrl = uploaded.Url;
        bike.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await OkAsync(motorcycleId, "Đã lưu ảnh.", ct);
    }

    public async Task<MediaMutationResult> ClearSlotAsync(Guid motorcycleId, MediaSlot slot, CancellationToken ct = default)
    {
        if (slot != MediaSlot.Thumbnail)
            return Fail("Slot không hợp lệ.");

        var bike = await db.Motorcycles.FirstOrDefaultAsync(m => m.Id == motorcycleId && !m.IsDeleted, ct);
        if (bike is null) return Fail("Không tìm thấy xe.");

        bike.ThumbnailUrl = null;
        bike.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await OkAsync(motorcycleId, "Đã xóa ảnh.", ct);
    }

    public async Task<MediaMutationResult> UpsertColorAsync(Guid motorcycleId, Guid? colorId, string name, string hex, MediaFileUpload? image, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Fail("Nhập tên màu.");
        hex = NormalizeHex(hex) ?? "#000000";
        if (!Regex.IsMatch(hex, "^#[0-9A-Fa-f]{6}$")) return Fail("Mã màu không hợp lệ (ví dụ #E40521).");

        if (colorId is null)
        {
            if (image is null) return Fail("Thêm ảnh đại diện cho màu.");
            var uploaded = await UploadValidatedAsync(image, Folder(motorcycleId, MediaSlot.Color), ct);
            if (!uploaded.Ok) return Fail(uploaded.Error!);
            var maxSort = await db.MotorcycleColors.Where(c => c.MotorcycleId == motorcycleId && !c.IsDeleted)
                .Select(c => (int?)c.SortOrder).MaxAsync(ct) ?? -1;
            db.MotorcycleColors.Add(new MotorcycleColor
            {
                MotorcycleId = motorcycleId,
                Name = name.Trim(),
                HexCode = hex,
                ImageUrl = uploaded.Url,
                SortOrder = maxSort + 1
            });
        }
        else
        {
            var color = await db.MotorcycleColors.FirstOrDefaultAsync(c => c.Id == colorId && c.MotorcycleId == motorcycleId && !c.IsDeleted, ct);
            if (color is null) return Fail("Không tìm thấy màu.");
            color.Name = name.Trim();
            color.HexCode = hex;
            if (image is not null)
            {
                var uploaded = await UploadValidatedAsync(image, Folder(motorcycleId, MediaSlot.Color), ct);
                if (!uploaded.Ok) return Fail(uploaded.Error!);
                color.ImageUrl = uploaded.Url;
            }
            color.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return await OkAsync(motorcycleId, "Đã lưu màu.", ct);
    }

    public async Task<MediaMutationResult> ReplaceColorImageAsync(Guid motorcycleId, Guid colorId, MediaFileUpload file, CancellationToken ct = default)
    {
        var color = await db.MotorcycleColors.FirstOrDefaultAsync(c => c.Id == colorId && c.MotorcycleId == motorcycleId && !c.IsDeleted, ct);
        if (color is null) return Fail("Không tìm thấy màu.");
        var uploaded = await UploadValidatedAsync(file, Folder(motorcycleId, MediaSlot.Color), ct);
        if (!uploaded.Ok) return Fail(uploaded.Error!);
        color.ImageUrl = uploaded.Url;
        color.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await OkAsync(motorcycleId, "Đã thay ảnh màu.", ct);
    }

    public async Task<MediaMutationResult> ReorderColorsAsync(Guid motorcycleId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default)
    {
        var colors = await db.MotorcycleColors.Where(c => c.MotorcycleId == motorcycleId && !c.IsDeleted).ToListAsync(ct);
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var color = colors.FirstOrDefault(c => c.Id == orderedIds[i]);
            if (color is null) continue;
            color.SortOrder = i;
            color.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return await OkAsync(motorcycleId, null, ct);
    }

    public async Task<MediaMutationResult> DeleteColorAsync(Guid motorcycleId, Guid colorId, CancellationToken ct = default)
    {
        var color = await db.MotorcycleColors.FirstOrDefaultAsync(c => c.Id == colorId && c.MotorcycleId == motorcycleId, ct);
        if (color is null) return Fail("Không tìm thấy màu.");
        color.IsDeleted = true;
        color.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await OkAsync(motorcycleId, "Đã xóa màu.", ct);
    }

    public async Task<MediaMutationResult> SetAngleAsync(Guid motorcycleId, MotorcycleViewAngle angle, MediaFileUpload file, CancellationToken ct = default)
    {
        if (!await BikeExistsAsync(motorcycleId, ct)) return Fail("Không tìm thấy xe.");
        if ((int)angle < 0 || (int)angle >= MotorcycleViewAngleCatalog.Count)
            return Fail("Góc xem không hợp lệ.");

        var uploaded = await UploadValidatedAsync(file, Folder(motorcycleId, MediaSlot.Angles), ct);
        if (!uploaded.Ok) return Fail(uploaded.Error!);

        var existing = await db.MotorcycleSpinFrames
            .FirstOrDefaultAsync(f => f.MotorcycleId == motorcycleId && f.Angle == angle && !f.IsDeleted, ct);

        if (existing is not null)
        {
            existing.ImageUrl = uploaded.Url!;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.MotorcycleSpinFrames.Add(new MotorcycleSpinFrame
            {
                MotorcycleId = motorcycleId,
                ImageUrl = uploaded.Url!,
                Angle = angle
            });
        }

        await db.SaveChangesAsync(ct);
        var label = MotorcycleViewAngleCatalog.Get(angle).LabelVi;
        return await OkAsync(motorcycleId, $"Đã cập nhật góc {label}.", ct);
    }

    public async Task<MediaMutationResult> ClearAngleAsync(Guid motorcycleId, MotorcycleViewAngle angle, CancellationToken ct = default)
    {
        var frames = await db.MotorcycleSpinFrames
            .Where(f => f.MotorcycleId == motorcycleId && f.Angle == angle && !f.IsDeleted)
            .ToListAsync(ct);
        if (frames.Count == 0) return Fail("Không tìm thấy góc xem.");
        foreach (var f in frames)
        {
            f.IsDeleted = true;
            f.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        var label = MotorcycleViewAngleCatalog.Get(angle).LabelVi;
        return await OkAsync(motorcycleId, $"Đã xóa góc {label}.", ct);
    }

    public async Task<MediaMutationResult> ClearAllAnglesAsync(Guid motorcycleId, CancellationToken ct = default)
    {
        var frames = await db.MotorcycleSpinFrames.Where(f => f.MotorcycleId == motorcycleId && !f.IsDeleted).ToListAsync(ct);
        foreach (var f in frames)
        {
            f.IsDeleted = true;
            f.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return await OkAsync(motorcycleId, "Đã xóa toàn bộ góc xem.", ct);
    }

    public async Task<SmartImportSummaryDto> SmartImportAsync(Guid motorcycleId, IReadOnlyList<MediaFileUpload> entries, CancellationToken ct = default)
    {
        if (!await BikeExistsAsync(motorcycleId, ct))
            return new SmartImportSummaryDto(false, "Không tìm thấy xe.", 0, 0, 0, [], null);

        var warnings = new List<string>();
        var thumb = 0;
        var colors = 0;
        var angles = 0;

        MediaFileUpload? pendingHero = null;
        var angleEntries = new List<(MotorcycleViewAngle Angle, MediaFileUpload File)>();
        var colorGroups = new Dictionary<string, List<MediaFileUpload>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var path = (entry.RelativePath ?? entry.FileName).Replace('\\', '/').Trim('/');
            var lower = path.ToLowerInvariant();
            var fileName = Path.GetFileName(path);

            if (IsGalleryPath(lower))
            {
                warnings.Add($"Bỏ qua gallery: {path}");
                continue;
            }

            if (IsThumbPath(lower, fileName))
            {
                var r = await SetSlotAsync(motorcycleId, MediaSlot.Thumbnail, entry, ct);
                if (r.Success) thumb++;
                else warnings.Add(r.Message ?? fileName);
            }
            else if (IsHeroPath(lower, fileName))
            {
                pendingHero = entry;
            }
            else if (IsAngleFolderPath(lower) || MotorcycleViewAngleCatalog.TryParseKey(fileName, out _))
            {
                if (!MotorcycleViewAngleCatalog.TryParseKey(fileName, out var angle)
                    && !MotorcycleViewAngleCatalog.TryParseKey(Path.GetFileNameWithoutExtension(fileName), out angle))
                {
                    warnings.Add($"Không nhận diện góc: {path}");
                    continue;
                }
                angleEntries.Add((angle, entry));
            }
            else if (TryColorFolder(lower, out var colorName))
            {
                if (!colorGroups.TryGetValue(colorName, out var list))
                    colorGroups[colorName] = list = [];
                list.Add(entry);
            }
            else if (lower.Contains("/colors/") || lower.StartsWith("colors/"))
            {
                var name = Path.GetFileNameWithoutExtension(fileName);
                if (!colorGroups.TryGetValue(name, out var list))
                    colorGroups[name] = list = [];
                list.Add(entry);
            }
            else
                warnings.Add($"Không nhận diện: {path}");
        }

        if (thumb == 0 && pendingHero is not null)
        {
            var bike = await db.Motorcycles.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == motorcycleId && !m.IsDeleted, ct);
            if (bike is not null && string.IsNullOrWhiteSpace(bike.ThumbnailUrl))
            {
                var r = await SetSlotAsync(motorcycleId, MediaSlot.Thumbnail, pendingHero, ct);
                if (r.Success) thumb++;
                else warnings.Add(r.Message ?? pendingHero.FileName);
            }
            else
                warnings.Add($"Bỏ qua hero.jpg (đã có ảnh đại diện).");
        }
        else if (pendingHero is not null)
            warnings.Add("Bỏ qua hero.jpg.");

        foreach (var (angle, file) in angleEntries)
        {
            var r = await SetAngleAsync(motorcycleId, angle, file, ct);
            if (r.Success) angles++;
            else warnings.Add(r.Message ?? file.FileName);
        }

        foreach (var (name, files) in colorGroups)
        {
            var image = files.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase).First();
            var hex = GuessHex(name);
            var r = await UpsertColorAsync(motorcycleId, null, ToTitle(name), hex, image, ct);
            if (r.Success) colors++;
            else warnings.Add($"{name}: {r.Message}");
        }

        var state = await GetStateAsync(motorcycleId, ct);
        return new SmartImportSummaryDto(
            true,
            $"Đã import: thumb {thumb}, màu {colors}, góc {angles}.",
            thumb, colors, angles, warnings, state);
    }

    public async Task<(bool Ok, string? Url, string? Error)> UploadOnlyAsync(MediaFileUpload file, string folder, CancellationToken ct = default)
    {
        var uploaded = await UploadValidatedAsync(file, folder, ct);
        return uploaded.Ok ? (true, uploaded.Url, null) : (false, null, uploaded.Error);
    }

    // ─── helpers ───────────────────────────────────────────────

    private async Task<Motorcycle?> LoadBikeAsync(Guid id, CancellationToken ct) =>
        await db.Motorcycles.AsNoTracking()
            .Include(m => m.Colors)
            .Include(m => m.SpinFrames)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);

    private Task<bool> BikeExistsAsync(Guid id, CancellationToken ct) =>
        db.Motorcycles.AnyAsync(m => m.Id == id && !m.IsDeleted, ct);

    private async Task<MediaMutationResult> OkAsync(Guid id, string? message, CancellationToken ct)
    {
        var state = await GetStateAsync(id, ct);
        return new MediaMutationResult(true, message, state);
    }

    private static MediaMutationResult Fail(string message) => new(false, message, null);

    private MediaStudioStateDto BuildState(Motorcycle bike)
    {
        var byAngle = bike.SpinFrames.Where(f => !f.IsDeleted)
            .GroupBy(f => f.Angle)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).First());

        var slots = MotorcycleViewAngleCatalog.All
            .Select(e =>
            {
                byAngle.TryGetValue(e.Angle, out var frame);
                return new AngleSlotDto(e.Key, e.LabelVi, (int)e.Angle, frame?.Id, frame?.ImageUrl);
            })
            .ToList();

        var filled = slots.Count(s => !string.IsNullOrWhiteSpace(s.Url));
        var complete = filled == MotorcycleViewAngleCatalog.Count;

        var colors = bike.Colors.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder)
            .Select(c => new ColorCardDto(c.Id, c.Name, c.HexCode, c.ImageUrl, c.SortOrder))
            .ToList();

        var health = BuildHealth(bike, colors, filled, complete);
        var publish = BuildPublish(bike, colors);

        string statusLabel;
        if (filled == 0) statusLabel = "Chưa có góc xem";
        else if (complete) statusLabel = $"Đủ {MotorcycleViewAngleCatalog.Count} góc";
        else
        {
            var missing = slots.Where(s => string.IsNullOrWhiteSpace(s.Url)).Select(s => s.Label).Take(4);
            statusLabel = $"{filled}/{MotorcycleViewAngleCatalog.Count} · Thiếu " + string.Join(", ", missing);
        }

        return new MediaStudioStateDto(
            bike.Id,
            bike.Name,
            bike.Slug,
            storage.SupportsUpload,
            storage.StorageDescription,
            string.IsNullOrWhiteSpace(bike.ThumbnailUrl) ? null : new MediaSlotDto(bike.ThumbnailUrl!, null, null, null, null),
            colors,
            new AngleStudioDto(slots, filled, MotorcycleViewAngleCatalog.Count, complete, statusLabel),
            health,
            publish);
    }

    private static MediaHealthDto BuildHealth(
        Motorcycle bike,
        List<ColorCardDto> colors,
        int filled,
        bool complete)
    {
        var hasThumb = !string.IsNullOrWhiteSpace(bike.ThumbnailUrl);
        var colorWithImage = colors.Count(c => !string.IsNullOrWhiteSpace(c.ImageUrl));
        var hasColor = colorWithImage > 0;

        var angleStatus = complete ? "ok" : "warn";
        var angleDetail = complete
            ? "Đủ 6 góc"
            : filled == 0
                ? "Chưa có (tuỳ chọn)"
                : $"{filled}/6 góc";

        var items = new List<MediaHealthItemDto>
        {
            new("thumbnail", "Ảnh đại diện", hasThumb ? "ok" : "bad", hasThumb ? "Có" : "Chưa có"),
            new("colors", "Ít nhất 1 màu có ảnh", hasColor ? "ok" : "bad",
                hasColor ? $"{colorWithImage} màu có ảnh" : "Chưa có"),
            new("angles", "6 góc xe", angleStatus, angleDetail)
        };

        // Ready blockers are thumb + color; angles are optional (warn only).
        var score = 0;
        if (hasThumb) score += 45;
        if (hasColor) score += 45;
        if (complete) score += 10;
        else if (filled > 0) score += 5;

        return new MediaHealthDto(Math.Clamp(score, 0, 100), items);
    }

    private static PublishReadinessDto BuildPublish(Motorcycle bike, List<ColorCardDto> colors)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(bike.ThumbnailUrl))
            missing.Add("Ảnh đại diện");
        if (!colors.Any(c => !string.IsNullOrWhiteSpace(c.ImageUrl)))
            missing.Add("Ít nhất 1 màu có ảnh");

        var ready = missing.Count == 0;
        return new PublishReadinessDto(
            ready,
            ready ? "Sẵn sàng đăng" : "Còn thiếu hình",
            missing);
    }

    private async Task<(bool Ok, string? Url, string? Error, int? Width, int? Height, long? Bytes)> UploadValidatedAsync(
        MediaFileUpload file, string folder, CancellationToken ct)
    {
        var err = Validate(file);
        if (err is not null) return (false, null, err, null, null, null);
        if (!storage.SupportsUpload)
            return (false, null, "Upload chưa sẵn sàng. Cấu hình Cloudinary (Production) hoặc Local (Development).", null, null, null);

        if (file.Content.CanSeek) file.Content.Position = 0;
        var result = await storage.UploadAsync(file.Content, file.FileName, file.ContentType, folder, ct);
        if (!result.Success)
            return (false, null, result.ErrorMessage ?? "Không tải được ảnh.", null, null, null);

        return (true, result.EffectiveUrl, null, result.Width, result.Height, result.Bytes ?? file.Length);
    }

    private string? Validate(MediaFileUpload file)
    {
        if (string.IsNullOrWhiteSpace(file.FileName)) return "Tên file không hợp lệ.";
        if (file.Length <= 0 && (!file.Content.CanSeek || file.Content.Length <= 0))
            return "File trống.";
        var len = file.Length > 0 ? file.Length : (file.Content.CanSeek ? file.Content.Length : 0);
        var max = options.Value.MaxFileSizeMb * 1024L * 1024L;
        if (len > max) return $"Ảnh vượt quá {options.Value.MaxFileSizeMb} MB.";
        var ct = string.IsNullOrWhiteSpace(file.ContentType) ? GuessContentType(file.FileName) : file.ContentType;
        if (!AllowedTypes.Contains(ct))
            return "Chỉ chấp nhận JPG, PNG, WebP, GIF hoặc SVG.";
        return null;
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

    private static string Folder(Guid id, MediaSlot slot) => slot switch
    {
        MediaSlot.Thumbnail => $"motorcycles/{id:N}/thumb",
        MediaSlot.Color => $"motorcycles/{id:N}/colors",
        MediaSlot.Angles => $"motorcycles/{id:N}/angles",
        _ => $"motorcycles/{id:N}"
    };

    private static string? NormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return "#000000";
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        return Regex.IsMatch(hex, "^#[0-9A-Fa-f]{6}$") ? hex.ToUpperInvariant() : null;
    }

    private static bool IsThumbPath(string lower, string fileName) =>
        lower is "thumbnail.jpg" or "thumbnail.jpeg" or "thumbnail.png" or "thumbnail.webp"
        || lower.EndsWith("/thumbnail.jpg") || lower.EndsWith("/thumbnail.jpeg")
        || lower.EndsWith("/thumbnail.png") || lower.EndsWith("/thumbnail.webp")
        || lower is "thumb.jpg" or "thumb.png" or "thumb.jpeg" or "thumb.webp"
        || fileName.Equals("thumbnail.jpg", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals("thumbnail.jpeg", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals("thumbnail.png", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals("thumbnail.webp", StringComparison.OrdinalIgnoreCase);

    private static bool IsHeroPath(string lower, string fileName) =>
        lower is "hero.jpg" or "hero.jpeg" or "hero.png" or "hero.webp"
        || lower.EndsWith("/hero.jpg") || lower.EndsWith("/hero.jpeg")
        || lower.EndsWith("/hero.png") || lower.EndsWith("/hero.webp")
        || fileName.Equals("hero.jpg", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals("hero.jpeg", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals("hero.png", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals("hero.webp", StringComparison.OrdinalIgnoreCase);

    private static bool IsGalleryPath(string lower) =>
        lower.Contains("/gallery/") || lower.StartsWith("gallery/");

    private static bool IsAngleFolderPath(string lower) =>
        lower.Contains("/angles/") || lower.StartsWith("angles/")
        || lower.Contains("/360/") || lower.StartsWith("360/")
        || lower.Contains("/spin/") || lower.StartsWith("spin/");

    private static bool TryColorFolder(string lower, out string colorName)
    {
        colorName = "";
        var m = Regex.Match(lower, @"colors/([^/]+)/");
        if (!m.Success) return false;
        colorName = m.Groups[1].Value;
        return !string.IsNullOrWhiteSpace(colorName) && colorName is not ("gallery" or "360" or "spin" or "angles");
    }

    private static string GuessHex(string name) => name.ToLowerInvariant() switch
    {
        "black" or "den" => "#111111",
        "white" or "trang" => "#F5F5F5",
        "red" or "do" => "#E40521",
        "blue" or "xanh" => "#1D4ED8",
        "gray" or "grey" or "xam" => "#6B7280",
        "silver" => "#C0C0C0",
        _ => "#333333"
    };

    private static string ToTitle(string name) =>
        string.Join(' ', name.Replace('-', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
}
