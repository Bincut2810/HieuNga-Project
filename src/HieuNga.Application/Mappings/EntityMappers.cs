using System.Text.Json;
using HieuNga.Application.DTOs;
using HieuNga.Domain;
using HieuNga.Domain.Common;
using HieuNga.Domain.Entities;

namespace HieuNga.Application.Mappings;



public static class EntityMappers

{

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };



    public static MotorcycleListItemDto ToListItem(this Motorcycle m)
    {
        var thumb = MotorcycleImageCatalog.ResolveThumbnail(m.ThumbnailUrl);
        var (available, label) = ResolveAvailability(m);
        return new(m.Id, m.Name, m.Slug, m.ShortDescription, m.Category, m.BasePrice, thumb, m.IsFeatured, available, label);
    }

    private static (bool IsAvailable, string Label) ResolveAvailability(Motorcycle m)
    {
        var variants = m.Variants?.Where(v => !v.IsDeleted).ToList() ?? [];
        if (variants.Count == 0)
            return (true, "Còn hàng");
        if (variants.Any(v => v.IsAvailable))
            return (true, "Còn hàng");
        return (false, "Hết hàng");
    }

    public static MotorcycleDetailDto ToDetail(this Motorcycle m)
    {
        // Thumbnail only for list; colors own detail hero images; angles from CMS.
        var thumb = MotorcycleImageCatalog.ResolveThumbnail(m.ThumbnailUrl);

        var highlights = ParseHighlights(m.HighlightsJson);
        var specifications = ParseSpecifications(m.TechnicalSpecsJson);
        if (specifications.Count == 0)
            specifications = BuildFallbackSpecifications(m);

        return new(
            m.Id, m.Name, m.Slug, m.ShortDescription, m.Description, m.Category, m.BasePrice,
            m.EngineCc, m.FuelType, m.Transmission, thumb,
            m.Variants.Where(v => !v.IsDeleted).Select(v => new MotorcycleVariantDto(v.Id, v.Name, v.Price, v.StockQuantity, v.IsAvailable)).ToList(),
            m.Colors.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder)
                .Select(c => new MotorcycleColorDto(c.Id, c.Name, c.HexCode, CmsImageOrNull(c.ImageUrl))).ToList(),
            highlights,
            specifications,
            (m.Features ?? []).Where(f => !f.IsDeleted).OrderBy(f => f.SortOrder)
                .Select(f => new MotorcycleFeatureDto(f.Id, f.Title, f.Description, f.ImageUrl, f.SortOrder)).ToList(),
            (m.Technologies ?? []).Where(t => !t.IsDeleted).OrderBy(t => t.SortOrder)
                .Select(t => new MotorcycleTechnologyDto(t.Id, t.Title, t.Description, t.ImageUrl, t.SortOrder)).ToList(),
            MapAngleImages(m),
            m.ToSeo());
    }



    /// <summary>
    /// Single mapping: CMS catalog order → filled angles only (same keys as Admin Media Studio).
    /// No FrameIndex, no sequence padding, no presentation remapping later.
    /// </summary>
    private static IReadOnlyList<MotorcycleAngleImageDto> MapAngleImages(Motorcycle m)
    {
        var byAngle = (m.SpinFrames ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.ImageUrl))
            .GroupBy(s => s.Angle)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).First().ImageUrl);

        return MotorcycleViewAngleCatalog.All
            .Where(e => byAngle.TryGetValue(e.Angle, out _))
            .Select(e => new MotorcycleAngleImageDto(e.Key, e.LabelVi, byAngle[e.Angle]))
            .ToList();
    }



    private static string? CmsImageOrNull(string? url) =>

        MotorcycleImageCatalog.IsValidImageUrl(url) ? url : null;



    public static SeoMetadataDto ToSeo(this ISeoEntity entity) => new(

        entity.MetaTitle, entity.MetaDescription, entity.MetaKeywords, entity.OgImageUrl, entity.CanonicalUrl);



    public static BannerDto ToDto(this Banner b) => new(
        b.Id,
        b.Title,
        b.Subtitle,
        b.ImageUrl,
        b.MobileImageUrl,
        b.CtaText,
        b.CtaUrl,
        b.SecondaryCtaText,
        b.SecondaryCtaUrl,
        b.Badge,
        Math.Clamp(b.OverlayStrength, 0, 100),
        b.TextAlignment);



    public static PromotionDto ToDto(this Promotion p) =>
        new(p.Id, p.Title, p.Slug, p.Summary, p.Type, CmsImageOrNull(p.ImageUrl), p.EndDate);

    public static PromotionDetailDto ToDetail(this Promotion p) => new(
        p.Id, p.Title, p.Slug, p.Summary, p.Content, p.Type, p.DiscountPercent, p.DiscountAmount,
        p.StartDate, p.EndDate, CmsImageOrNull(p.ImageUrl),
        p.Motorcycle?.Name, p.Motorcycle?.Slug, p.ToSeo());

    public static BranchDto ToDto(this Branch b) =>
        new(b.Id, b.Name, b.Address, b.Phone, b.Hotline, b.Email, b.MapEmbedUrl, b.OpeningHours, b.IsHeadOffice, b.Slug);

    public static ReviewDto ToDto(this Review r, string? motorcycleName = null) =>
        new(r.Id, r.CustomerName, r.Rating, r.Title, r.Content, motorcycleName);

    public static BlogPostListItemDto ToListItem(this BlogPost p) =>
        new(p.Id, p.Title, p.Slug, p.Summary, CmsImageOrNull(p.ThumbnailUrl), p.PublishedAt);

    public static BlogDetailDto ToDetail(this BlogPost p) => new(
        p.Id, p.Title, p.Slug, p.Summary, p.Content, CmsImageOrNull(p.ThumbnailUrl),
        p.Category?.Name, p.AuthorName, p.PublishedAt, p.ToSeo());



    private static IReadOnlyList<string> ParseHighlights(string? json)

    {

        if (string.IsNullOrWhiteSpace(json)) return [];

        try

        {

            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];

        }

        catch { return []; }

    }



    private static IReadOnlyList<MotorcycleSpecItemDto> ParseSpecifications(string? json)

    {

        if (string.IsNullOrWhiteSpace(json)) return [];

        try

        {

            var items = JsonSerializer.Deserialize<List<SpecJson>>(json, JsonOptions);

            return items?.Select(x => new MotorcycleSpecItemDto(x.Icon ?? "•", x.Label ?? "", x.Value ?? "")).ToList() ?? [];

        }

        catch { return []; }

    }



    private sealed class SpecJson

    {

        public string? Icon { get; set; }

        public string? Label { get; set; }

        public string? Value { get; set; }

    }



    private static IReadOnlyList<MotorcycleSpecItemDto> BuildFallbackSpecifications(Motorcycle m) =>

    [

        new("⚡", "Dung tích xy-lanh", m.EngineCc.HasValue ? $"{m.EngineCc} cc" : "Liên hệ showroom"),

        new("⚙️", "Hộp số", m.Transmission ?? "—"),

        new("⛽", "Nhiên liệu", m.FuelType ?? "Xăng"),

        new("🏷️", "Phân khúc", m.Category.ToString())

    ];

}


