using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HieuNga.Infrastructure.Persistence;

/// <summary>
/// Ensures flagship homepage hero banners exist for demo/dev.
/// Never duplicates by title; never overwrites CMS-edited fields.
/// </summary>
public static class HieuNgaHeroBannerSeed
{
    private static readonly HeroDef[] Specs =
    [
        new(
            "Khám phá Honda tại Đà Nẵng",
            "Showroom HEAD — xe chính hãng, tư vấn trả góp và lái thử.",
            "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=1920&q=80",
            "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=900&q=80",
            "Xem xe đang bán",
            "/xe",
            "Tính trả góp",
            "/tra-gop",
            "Xe Máy Hiếu Nga · HEAD Đà Nẵng",
            68,
            BannerTextAlignment.Left,
            0),
        new(
            "Trả góp linh hoạt tại HEAD",
            "Hỗ trợ nhiều ngân hàng — duyệt nhanh, lãi suất ưu đãi.",
            "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=1920&q=80",
            "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=900&q=80",
            "Tính trả góp",
            "/tra-gop",
            "Liên hệ tư vấn",
            "/lien-he?intent=tra-gop&source=hero",
            "Tài chính · Trả góp",
            62,
            BannerTextAlignment.Left,
            1),
        new(
            "Dịch vụ HEAD chuyên nghiệp",
            "Bảo dưỡng, sửa chữa và phụ tùng chính hãng Honda.",
            "https://images.unsplash.com/photo-1486754735734-4154f0c0a0c6?w=1920&q=80",
            "https://images.unsplash.com/photo-1486754735734-4154f0c0a0c6?w=900&q=80",
            "Xem dịch vụ",
            "/dich-vu",
            "Đặt lịch",
            "/bao-duong#booking",
            "Dịch vụ HEAD",
            70,
            BannerTextAlignment.Left,
            2),
    ];

    public static async Task EnsureAsync(HieuNgaDbContext context, ILogger logger, CancellationToken ct = default)
    {
        var heroes = await context.Banners
            .Where(b => !b.IsDeleted && b.Position == BannerPosition.Hero)
            .ToListAsync(ct);

        var changed = false;

        foreach (var def in Specs)
        {
            var match = heroes.FirstOrDefault(b =>
                string.Equals(b.Title, def.Title, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                var entity = new Banner
                {
                    Title = def.Title,
                    Subtitle = def.Subtitle,
                    ImageUrl = def.ImageUrl,
                    MobileImageUrl = def.MobileImageUrl,
                    CtaText = def.CtaText,
                    CtaUrl = def.CtaUrl,
                    SecondaryCtaText = def.SecondaryCtaText,
                    SecondaryCtaUrl = def.SecondaryCtaUrl,
                    Badge = def.Badge,
                    OverlayStrength = def.OverlayStrength,
                    TextAlignment = def.TextAlignment,
                    Position = BannerPosition.Hero,
                    SortOrder = def.SortOrder,
                    IsActive = true
                };
                context.Banners.Add(entity);
                heroes.Add(entity);
                changed = true;
                logger.LogInformation("Seeded hero banner {Title}", def.Title);
                continue;
            }

            if (FillEmptyHeroFields(match, def))
            {
                changed = true;
                logger.LogInformation("Filled empty hero fields on banner {Title}", match.Title);
            }
        }

        if (changed)
            await context.SaveChangesAsync(ct);
    }

    private static bool FillEmptyHeroFields(Banner b, HeroDef def)
    {
        var changed = false;
        if (string.IsNullOrWhiteSpace(b.Subtitle)) { b.Subtitle = def.Subtitle; changed = true; }
        if (string.IsNullOrWhiteSpace(b.MobileImageUrl)) { b.MobileImageUrl = def.MobileImageUrl; changed = true; }
        if (string.IsNullOrWhiteSpace(b.SecondaryCtaUrl))
        {
            b.SecondaryCtaUrl = def.SecondaryCtaUrl;
            b.SecondaryCtaText ??= def.SecondaryCtaText;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(b.Badge)) { b.Badge = def.Badge; changed = true; }
        if (b.OverlayStrength <= 0) { b.OverlayStrength = def.OverlayStrength; changed = true; }
        return changed;
    }

    private sealed record HeroDef(
        string Title,
        string Subtitle,
        string ImageUrl,
        string MobileImageUrl,
        string CtaText,
        string CtaUrl,
        string SecondaryCtaText,
        string SecondaryCtaUrl,
        string Badge,
        int OverlayStrength,
        BannerTextAlignment TextAlignment,
        int SortOrder);
}
