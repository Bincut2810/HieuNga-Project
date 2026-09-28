using System.Text.Json;
using HieuNga.Application.Media;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Infrastructure.Services;

/// <summary>
/// Domain service for the per-service gallery. Owns the <c>ServiceItem.GalleryJson</c>
/// array (append, delete by index, reorder by index) and the settings save.
/// Does NOT own the upload pipeline — the canonical
/// <see cref="Interfaces.IImageUploadService"/> produces URLs first, then
/// <see cref="AddImagesAsync"/> appends them to the array.
/// </summary>
public class ServiceCmsService(
    HieuNgaDbContext db,
    IUnitOfWork uow) : IServiceCmsService
{
    public async Task<ServiceCmsStateDto?> GetStateAsync(Guid serviceId, CancellationToken ct = default)
    {
        var item = await LoadAsync(serviceId, ct);
        return item is null ? null : BuildState(item);
    }

    public async Task<ServiceMutationResult> AddImagesAsync(Guid serviceId, IReadOnlyList<string> urls, CancellationToken ct = default)
    {
        if (urls.Count == 0)
            return Fail("Không có URL ảnh để thêm.");

        var item = await LoadAsync(serviceId, ct);
        if (item is null)
            return Fail("Không tìm thấy dịch vụ.");

        var images = ServiceGallery.Parse(item.GalleryJson).ToList();
        var added = 0;
        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            images.Add(url.Trim());
            added++;
        }

        if (added == 0)
            return Fail("Danh sách URL rỗng hoặc không hợp lệ.");

        item.GalleryJson = ServiceGallery.Serialize(images);
        item.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Ok($"Đã thêm {added} ảnh.", item);
    }

    public async Task<ServiceMutationResult> DeleteImageAsync(Guid serviceId, int index, CancellationToken ct = default)
    {
        var item = await LoadAsync(serviceId, ct);
        if (item is null)
            return Fail("Không tìm thấy dịch vụ.");

        var images = ServiceGallery.Parse(item.GalleryJson).ToList();
        if (index < 0 || index >= images.Count)
            return Fail("Không tìm thấy ảnh.");

        images.RemoveAt(index);
        item.GalleryJson = ServiceGallery.Serialize(images);
        item.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Ok("Đã xóa ảnh.", item);
    }

    public async Task<ServiceMutationResult> ReorderImagesAsync(Guid serviceId, IReadOnlyList<int> orderedIndexes, CancellationToken ct = default)
    {
        var item = await LoadAsync(serviceId, ct);
        if (item is null)
            return Fail("Không tìm thấy dịch vụ.");

        var current = ServiceGallery.Parse(item.GalleryJson);
        if (orderedIndexes.Count == 0 || current.Count == 0)
            return Ok(null, item);

        var next = new List<string>(orderedIndexes.Count);
        foreach (var i in orderedIndexes)
        {
            if (i < 0 || i >= current.Count) continue;
            next.Add(current[i]);
        }

        if (next.Count != current.Count)
            return Fail("Thứ tự ảnh không hợp lệ.");

        item.GalleryJson = ServiceGallery.Serialize(next);
        item.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Ok("Đã cập nhật thứ tự.", item);
    }

    public async Task<ServiceMutationResult> SaveSettingsAsync(
        Guid serviceId,
        string name,
        string? shortDescription,
        int displayOrder,
        bool enabled,
        CancellationToken ct = default)
    {
        var item = await LoadAsync(serviceId, ct);
        if (item is null)
            return Fail("Không tìm thấy dịch vụ.");

        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return Fail("Vui lòng nhập tên dịch vụ.");

        var slug = SlugHelper.Generate(trimmed);
        if (await db.ServiceItems.AnyAsync(s => s.Slug == slug && s.Id != serviceId && !s.IsDeleted, ct))
            slug = $"{slug}-{serviceId.ToString("N")[..6]}";

        item.Name = trimmed;
        item.Slug = slug;
        item.ShortDescription = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        item.DisplayOrder = displayOrder;
        item.IsActive = enabled;
        item.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        return Ok(enabled ? "Đã lưu và xuất bản." : "Đã lưu bản nháp.", item);
    }

    private async Task<ServiceItem?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.ServiceItems.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

    private static ServiceCmsStateDto BuildState(ServiceItem item)
    {
        var images = ServiceGallery.Parse(item.GalleryJson)
            .Select((url, i) => new ServiceCmsImageDto(i, url))
            .ToList();
        return new ServiceCmsStateDto(item.Id, item.Name, item.ShortDescription, item.DisplayOrder, item.IsActive, images);
    }

    private static ServiceMutationResult Ok(string? message, ServiceItem item) =>
        new(true, message, BuildState(item));

    private static ServiceMutationResult Fail(string message) =>
        new(false, message, null);
}

public static class ServiceGallery
{
    public static IReadOnlyList<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return (JsonSerializer.Deserialize<List<string>>(json) ?? [])
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<string> urls)
    {
        var list = urls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToList();
        return list.Count == 0 ? null : JsonSerializer.Serialize(list);
    }
}
