using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class InstallmentRequest : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid? MotorcycleId { get; set; }
    public decimal VehiclePrice { get; set; }
    public decimal DownPayment { get; set; }
    public int TermMonths { get; set; }
    public decimal? MonthlyPayment { get; set; }
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public bool IsProcessed { get; set; }

    public Motorcycle? Motorcycle { get; set; }
}
