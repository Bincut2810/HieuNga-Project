using System.Text.Json;

using HieuNga.Application.DTOs;

using HieuNga.Domain.Common;

using HieuNga.Domain.Entities;



namespace HieuNga.Application.Mappings;



public static class EntityMappers

{

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };



    public static MotorcycleListItemDto ToListItem(this Motorcycle m)

    {

        var mediaUrl = m.MediaAssets?.Where(a => !a.IsDeleted).OrderBy(a => a.SortOrder).FirstOrDefault()?.Url;

        var thumb = MotorcycleImageCatalog.ResolveThumbnail(m.Slug, m.ThumbnailUrl, mediaUrl);

        return new(m.Id, m.Name, m.Slug, m.ShortDescription, m.Category, m.BasePrice, thumb, m.IsFeatured);

    }



    public static MotorcycleDetailDto ToDetail(this Motorcycle m)

    {

        var mediaUrls = m.MediaAssets.Where(a => !a.IsDeleted).OrderBy(a => a.SortOrder).Select(a => a.Url);

        var gallery = MotorcycleImageCatalog.ResolveGallery(m.Slug, mediaUrls);

        var thumb = MotorcycleImageCatalog.ResolveThumbnail(m.Slug, m.ThumbnailUrl, gallery.FirstOrDefault());



        var highlights = ParseHighlights(m.HighlightsJson);

        var specifications = ParseSpecifications(m.TechnicalSpecsJson);

        if (specifications.Count == 0)

            specifications = BuildFallbackSpecifications(m);



        return new(

            m.Id, m.Name, m.Slug, m.ShortDescription, m.Description, m.Category, m.BasePrice,

            m.EngineCc, m.FuelType, m.Transmission, thumb,

            m.Variants.Where(v => !v.IsDeleted).Select(v => new MotorcycleVariantDto(v.Id, v.Name, v.Price, v.StockQuantity, v.IsAvailable)).ToList(),

            m.Colors.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder).Select(c => new MotorcycleColorDto(c.Id, c.Name, c.HexCode, ResolveColorImage(c.ImageUrl, m.Slug))).ToList(),

            gallery,

            highlights,

            specifications,

            m.ToSeo());

    }



    private static string? ResolveColorImage(string? url, string slug) =>

        MotorcycleImageCatalog.IsValidImageUrl(url) ? url : MotorcycleImageCatalog.GetThumbnail(slug);



    public static SeoMetadataDto ToSeo(this ISeoEntity entity) => new(

        entity.MetaTitle, entity.MetaDescription, entity.MetaKeywords, entity.OgImageUrl, entity.CanonicalUrl);



    public static BannerDto ToDto(this Banner b) => new(b.Id, b.Title, b.Subtitle, b.ImageUrl, b.MobileImageUrl, b.CtaText, b.CtaUrl);



    public static PromotionDto ToDto(this Promotion p) => new(p.Id, p.Title, p.Slug, p.Summary, p.Type, p.ImageUrl ?? GetPromotionPlaceholder(p.Type), p.EndDate);



    public static PromotionDetailDto ToDetail(this Promotion p) => new(

        p.Id, p.Title, p.Slug, p.Summary, p.Content, p.Type, p.DiscountPercent, p.DiscountAmount,

        p.StartDate, p.EndDate, p.ImageUrl ?? GetPromotionPlaceholder(p.Type),

        p.Motorcycle?.Name, p.Motorcycle?.Slug, p.ToSeo());



    public static BranchDto ToDto(this Branch b) => new(b.Id, b.Name, b.Address, b.Phone, b.Hotline, b.Email, b.MapEmbedUrl, b.OpeningHours, b.IsHeadOffice);



    public static ReviewDto ToDto(this Review r, string? motorcycleName = null) =>

        new(r.Id, r.CustomerName, r.Rating, r.Title, r.Content, motorcycleName);



    public static BlogPostListItemDto ToListItem(this BlogPost p) =>

        new(p.Id, p.Title, p.Slug, p.Summary, p.ThumbnailUrl ?? GetBlogPlaceholder(p.Slug), p.PublishedAt);



    public static BlogDetailDto ToDetail(this BlogPost p) => new(

        p.Id, p.Title, p.Slug, p.Summary, p.Content, p.ThumbnailUrl ?? GetBlogPlaceholder(p.Slug),

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



    private static string GetPromotionPlaceholder(Domain.Enums.PromotionType type) => type switch

    {

        Domain.Enums.PromotionType.Financing => "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=800&q=80",

        Domain.Enums.PromotionType.Gift => "https://images.unsplash.com/photo-1605559424843-9e4c228ef1e2?w=800&q=80",

        _ => MotorcycleImageCatalog.Default

    };



    private static string GetBlogPlaceholder(string slug) =>

        $"https://images.unsplash.com/photo-1558980664-769d9df238f8?w=800&q=80&sig={slug.GetHashCode()}";



    private static IReadOnlyList<MotorcycleSpecItemDto> BuildFallbackSpecifications(Motorcycle m) =>

    [

        new("⚡", "Dung tích xy-lanh", m.EngineCc.HasValue ? $"{m.EngineCc} cc" : "Liên hệ showroom"),

        new("⚙️", "Hộp số", m.Transmission ?? "—"),

        new("⛽", "Nhiên liệu", m.FuelType ?? "Xăng"),

        new("🏷️", "Phân khúc", m.Category.ToString())

    ];

}


