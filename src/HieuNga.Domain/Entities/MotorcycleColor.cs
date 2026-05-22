using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class MotorcycleColor : BaseEntity
{
    public Guid MotorcycleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HexCode { get; set; } = "#000000";
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }

    public Motorcycle Motorcycle { get; set; } = null!;
}
