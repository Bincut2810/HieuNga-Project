using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class MotorcycleSpinFrame : BaseEntity
{
    public Guid MotorcycleId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int FrameIndex { get; set; }

    public Motorcycle Motorcycle { get; set; } = null!;
}
