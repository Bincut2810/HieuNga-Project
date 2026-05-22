using HieuNga.Domain.Common;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Entities;

public class Booking : BaseEntity
{
    public BookingType Type { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime PreferredDate { get; set; }
    public string? PreferredTime { get; set; }
    public string? Notes { get; set; }
    public Guid? MotorcycleId { get; set; }
    public Guid? BranchId { get; set; }

    public Motorcycle? Motorcycle { get; set; }
    public Branch? Branch { get; set; }
}
