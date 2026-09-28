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

/// <summary>
/// Default implementation of <see cref="IMotorcycleMediaStudioService"/>. The
/// service no longer touches the upload pipeline directly — it only manages the
/// database state for the motorcycle's thumbnail, color cards (with images),
/// and six fixed viewing-angle spin frames.
/// </summary>
public sealed class MotorcycleMediaStudioService(
    HieuNgaDbContext db,
    IImageStorageService storage,
    IOptions<ImageStorageOptions> options) : IMotorcycleMediaStudioService
{
    public async Task<MediaStudioStateDto?> GetStateAsync(Guid motorcycleId, CancellationToken ct = default)
    {
        var bike = await LoadBikeAsync(motorcycleId, ct);
        return bike is null ? null : BuildState(bike);
    }

    public async Task<MediaMutationResult> SetSlotUrlAsync(Guid motorcycleId, MediaSlot slot, string url, CancellationToken ct = default)
    {
        if (slot != MediaSlot.Thumbnail)
            return Fail("Slot không hợp lệ.");

        var bike = await db.Motorcycles.FirstOrDefaultAsync(m => m.Id == motorcycleId && !m.IsDeleted, ct);
        if (bike is null) return Fail("Không tìm thấy xe.");

        bike.ThumbnailUrl = url;
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

    public async Task<MediaMutationResult> UpsertColorUrlAsync(Guid motorcycleId, Guid? colorId, string name, string hex, string? imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Fail("Nhập tên màu.");
        hex = NormalizeHex(hex) ?? "#000000";
        if (!Regex.IsMatch(hex, "^#[0-9A-Fa-f]{6}$")) return Fail("Mã màu không hợp lệ (ví dụ #E40521).");

        if (colorId is null)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return Fail("Thêm ảnh đại diện cho màu.");
            var maxSort = await db.MotorcycleColors.Where(c => c.MotorcycleId == motorcycleId && !c.IsDeleted)
                .Select(c => (int?)c.SortOrder).MaxAsync(ct) ?? -1;
            db.MotorcycleColors.Add(new MotorcycleColor
            {
                MotorcycleId = motorcycleId,
                Name = name.Trim(),
                HexCode = hex,
                ImageUrl = imageUrl,
                SortOrder = maxSort + 1
            });
        }
        else
        {
            var color = await db.MotorcycleColors.FirstOrDefaultAsync(c => c.Id == colorId && c.MotorcycleId == motorcycleId && !c.IsDeleted, ct);
            if (color is null) return Fail("Không tìm thấy màu.");
            color.Name = name.Trim();
            color.HexCode = hex;
            if (!string.IsNullOrWhiteSpace(imageUrl))
                color.ImageUrl = imageUrl;
            color.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return await OkAsync(motorcycleId, "Đã lưu màu.", ct);
    }

    public async Task<MediaMutationResult> ReplaceColorImageUrlAsync(Guid motorcycleId, Guid colorId, string imageUrl, CancellationToken ct = default)
    {
        var color = await db.MotorcycleColors.FirstOrDefaultAsync(c => c.Id == colorId && c.MotorcycleId == motorcycleId && !c.IsDeleted, ct);
        if (color is null) return Fail("Không tìm thấy màu.");
        color.ImageUrl = imageUrl;
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

    public async Task<MediaMutationResult> SetAngleUrlAsync(Guid motorcycleId, MotorcycleViewAngle angle, string url, CancellationToken ct = default)
    {
        if (!await BikeExistsAsync(motorcycleId, ct)) return Fail("Không tìm thấy xe.");
        if ((int)angle < 0 || (int)angle >= MotorcycleViewAngleCatalog.Count)
            return Fail("Góc xem không hợp lệ.");

        var existing = await db.MotorcycleSpinFrames
            .FirstOrDefaultAsync(f => f.MotorcycleId == motorcycleId && f.Angle == angle && !f.IsDeleted, ct);

        if (existing is not null)
        {
            existing.ImageUrl = url;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.MotorcycleSpinFrames.Add(new MotorcycleSpinFrame
            {
                MotorcycleId = motorcycleId,
                ImageUrl = url,
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

    public async Task<MediaMutationResult> AssignByPathAsync(Guid motorcycleId, string relativePath, string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Fail("Thiếu đường dẫn.");
        var lower = relativePath.Replace('\\', '/').Trim('/').ToLowerInvariant();
        var fileName = Path.GetFileName(lower);

        if (fileName is "thumbnail.jpg" or "thumbnail.jpeg" or "thumbnail.png" or "thumbnail.webp"
            or "thumb.jpg" or "thumb.jpeg" or "thumb.png" or "thumb.webp")
        {
            return await SetSlotUrlAsync(motorcycleId, MediaSlot.Thumbnail, url, ct);
        }

        if (MotorcycleViewAngleCatalog.TryParseKey(fileName, out var angle)
            || MotorcycleViewAngleCatalog.TryParseKey(Path.GetFileNameWithoutExtension(fileName), out angle))
        {
            return await SetAngleUrlAsync(motorcycleId, angle, url, ct);
        }

        if (lower.Contains("/colors/") || lower.StartsWith("colors/") || lower.Contains("/gallery/") || lower.StartsWith("gallery/"))
        {
            var nameGuess = Path.GetFileNameWithoutExtension(fileName);
            var titleName = ToTitle(nameGuess);
            var hex = GuessHex(nameGuess);
            return await UpsertColorUrlAsync(motorcycleId, null, titleName, hex, url, ct);
        }

        return Fail($"Không nhận diện đường dẫn: {relativePath}");
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

    private static string? NormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return "#000000";
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        return Regex.IsMatch(hex, "^#[0-9A-Fa-f]{6}$") ? hex.ToUpperInvariant() : null;
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
