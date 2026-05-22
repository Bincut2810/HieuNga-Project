using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class Review : BaseEntity
{
    public Guid MotorcycleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsFeatured { get; set; }

    public Motorcycle Motorcycle { get; set; } = null!;
}
