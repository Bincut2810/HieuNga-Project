using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class MotorcycleVariant : BaseEntity
{
    public Guid MotorcycleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? Sku { get; set; }
    public bool IsAvailable { get; set; } = true;

    public Motorcycle Motorcycle { get; set; } = null!;
}
