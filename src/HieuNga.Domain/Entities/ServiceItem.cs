using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class ServiceItem : BaseEntity
{
    public Guid ServiceCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    /// <summary>Ordered gallery image URLs (JSON string array). Single image owner.</summary>
    public string? GalleryJson { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ServiceCategory Category { get; set; } = null!;
}
