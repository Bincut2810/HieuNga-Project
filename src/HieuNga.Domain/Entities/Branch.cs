using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? District { get; set; }
    public string City { get; set; } = "Đà Nẵng";
    public string? Phone { get; set; }
    public string? Hotline { get; set; }
    public string? Email { get; set; }
    public string? MapEmbedUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? OpeningHours { get; set; }
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];
}
