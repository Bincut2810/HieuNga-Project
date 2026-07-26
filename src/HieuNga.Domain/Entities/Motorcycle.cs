using HieuNga.Domain.Common;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Entities;

public class Motorcycle : BaseEntity, ISeoEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public MotorcycleCategory Category { get; set; }
    public decimal BasePrice { get; set; }
    public int? EngineCc { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    /// <summary>JSON array of highlight strings.</summary>
    public string? HighlightsJson { get; set; }
    /// <summary>JSON array of { icon, label, value } specification items.</summary>
    public string? TechnicalSpecsJson { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; }
    public string? ThumbnailUrl { get; set; }
    /// <summary>Optional dedicated hero / banner image for the detail page.</summary>
    public string? HeroImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }

    public ICollection<MotorcycleVariant> Variants { get; set; } = [];
    public ICollection<MotorcycleColor> Colors { get; set; } = [];
    public ICollection<MediaAsset> MediaAssets { get; set; } = [];
    public ICollection<MotorcycleFeature> Features { get; set; } = [];
    public ICollection<MotorcycleTechnology> Technologies { get; set; } = [];
    public ICollection<MotorcycleSpinFrame> SpinFrames { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
