using HieuNga.Domain.Common;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Entities;

/// <summary>One of six fixed motorcycle viewing angles (not a numbered spin frame).</summary>
public class MotorcycleSpinFrame : BaseEntity
{
    public Guid MotorcycleId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    /// <summary>Fixed angle slot 0–5 (<see cref="MotorcycleViewAngle"/>).</summary>
    public MotorcycleViewAngle Angle { get; set; }

    public Motorcycle Motorcycle { get; set; } = null!;
}
