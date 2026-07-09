using System.ComponentModel.DataAnnotations;
using HieuNga.Domain.Enums;

namespace HieuNga.Web.Pages.Admin.Xe;

public class MotorcycleInputModel : IAdminSeoInput
{
    [Required(ErrorMessage = "Vui lòng nhập tên xe")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Slug { get; set; }

    [Required]
    public MotorcycleCategory Category { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    [StringLength(300)]
    public string? MetaKeywords { get; set; }

    [StringLength(500)]
    public string? OgImageUrl { get; set; }

    [StringLength(500)]
    public string? CanonicalUrl { get; set; }
}
