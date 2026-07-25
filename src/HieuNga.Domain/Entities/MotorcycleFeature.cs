using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class MotorcycleFeature : BaseEntity
{
    public Guid MotorcycleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Motorcycle Motorcycle { get; set; } = null!;
}
