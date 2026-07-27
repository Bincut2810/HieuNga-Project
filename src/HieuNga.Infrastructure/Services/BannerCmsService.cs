using HieuNga.Application.Media;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Infrastructure.Services;

public class BannerCmsService(
    HieuNgaDbContext db,
    IUnitOfWork uow,
    IMotorcycleMediaStudioService mediaStudio) : IBannerCmsService
{
    public async Task<BannerCmsStateDto> GetStateAsync(CancellationToken ct = default)
    {
        var banners = await LoadBannersAsync(ct);
        return BuildState(banners);
    }

    public async Task<BannerMutationResult> UploadImagesAsync(IReadOnlyList<MediaFileUpload> files, CancellationToken ct = default)
    {
        if (files.Count == 0)
            return Fail("Chưa chọn ảnh.");

        var banners = await LoadBannersAsync(ct);
        var meta = MetaFrom(banners);
        var nextOrder = banners.Count > 0 ? banners.Max(b => b.SortOrder) + 1 : 0;
        var added = 0;

        foreach (var file in files)
        {
            var (ok, url, error) = await mediaStudio.UploadOnlyAsync(file, "banners", ct);
            if (!ok || string.IsNullOrWhiteSpace(url))
                return Fail(error ?? "Không tải được ảnh.");

            await db.Banners.AddAsync(new Banner
            {
                Title = meta.Title,
                Subtitle = meta.Subtitle,
                ImageUrl = url,
                SortOrder = nextOrder++,
                IsActive = meta.Enabled
            }, ct);
            added++;
        }

        await uow.SaveChangesAsync(ct);
        return Ok($"Đã tải {added} ảnh.", await LoadBannersAsync(ct));
    }

    public async Task<BannerMutationResult> DeleteImageAsync(Guid id, CancellationToken ct = default)
    {
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);
        if (banner is null)
            return Fail("Không tìm thấy ảnh.");

        banner.IsDeleted = true;
        await uow.SaveChangesAsync(ct);
        await NormalizeOrderAsync(ct);
        return Ok("Đã xóa ảnh.", await LoadBannersAsync(ct));
    }

    public async Task<BannerMutationResult> ReorderImagesAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct = default)
    {
        if (orderedIds.Count == 0)
            return Ok(null, await LoadBannersAsync(ct));

        var banners = await LoadBannersAsync(ct);
        var byId = banners.ToDictionary(b => b.Id);
        for (var i = 0; i < orderedIds.Count; i++)
        {
            if (!byId.TryGetValue(orderedIds[i], out var banner)) continue;
            banner.SortOrder = i;
        }

        await uow.SaveChangesAsync(ct);
        return Ok("Đã cập nhật thứ tự.", await LoadBannersAsync(ct));
    }

    public async Task<BannerMutationResult> SaveSettingsAsync(string title, string? subtitle, bool enabled, CancellationToken ct = default)
    {
        var banners = await LoadBannersAsync(ct);
        if (banners.Count == 0)
            return Fail("Tải ít nhất một ảnh trước khi lưu.");

        var trimmed = title.Trim();
        foreach (var banner in banners)
        {
            banner.Title = trimmed;
            banner.Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();
            banner.IsActive = enabled;
        }

        await uow.SaveChangesAsync(ct);
        return Ok("Đã lưu banner trang chủ.", banners);
    }

    private async Task<List<Banner>> LoadBannersAsync(CancellationToken ct) =>
        await db.Banners
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.CreatedAt)
            .ToListAsync(ct);

    private async Task NormalizeOrderAsync(CancellationToken ct)
    {
        var banners = await LoadBannersAsync(ct);
        for (var i = 0; i < banners.Count; i++)
            banners[i].SortOrder = i;
        await uow.SaveChangesAsync(ct);
    }

    private static (string Title, string? Subtitle, bool Enabled) MetaFrom(IReadOnlyList<Banner> banners)
    {
        if (banners.Count == 0)
            return ("", null, true);
        var first = banners[0];
        return (first.Title, first.Subtitle, first.IsActive);
    }

    private static BannerCmsStateDto BuildState(IReadOnlyList<Banner> banners)
    {
        var meta = MetaFrom(banners);
        var images = banners
            .Select(b => new BannerImageDto(b.Id, b.ImageUrl, b.SortOrder))
            .ToList();
        return new BannerCmsStateDto(meta.Title, meta.Subtitle, meta.Enabled, images);
    }

    private static BannerMutationResult Ok(string? message, IReadOnlyList<Banner> banners) =>
        new(true, message, BuildState(banners));

    private static BannerMutationResult Fail(string message) =>
        new(false, message, null);
}
