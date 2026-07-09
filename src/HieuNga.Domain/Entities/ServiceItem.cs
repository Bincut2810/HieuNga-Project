using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class ServiceItem : BaseEntity, ISeoEntity
{
    public Guid ServiceCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? DetailDescription { get; set; }
    /// <summary>JSON array of included service lines.</summary>
    public string? IncludesJson { get; set; }
    public string EstimatedPriceText { get; set; } = string.Empty;
    public string? EstimatedDurationText { get; set; }
    public string? PriceNote { get; set; }
    public string? IconKey { get; set; }
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
