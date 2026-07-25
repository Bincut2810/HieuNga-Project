using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class ServiceItem : BaseEntity, ISeoEntity
{
    public Guid ServiceCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? DetailDescription { get; set; }
    /// <summary>JSON array of included / benefit lines.</summary>
    public string? IncludesJson { get; set; }
    public string EstimatedPriceText { get; set; } = string.Empty;
    public string? EstimatedDurationText { get; set; }
    public string? PriceNote { get; set; }
    public string? IconKey { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? HeroImageUrl { get; set; }
    /// <summary>JSON array of gallery image URLs.</summary>
    public string? GalleryJson { get; set; }
    /// <summary>JSON array of FAQ objects: [{ "q": "...", "a": "..." }].</summary>
    public string? FaqJson { get; set; }
    /// <summary>JSON array of "when to use" bullets.</summary>
    public string? WhenToUseJson { get; set; }
    /// <summary>JSON array of work-process step strings.</summary>
    public string? ProcessJson { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }

    public ServiceCategory Category { get; set; } = null!;
}
