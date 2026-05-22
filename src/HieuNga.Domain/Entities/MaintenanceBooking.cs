using HieuNga.Domain.Common;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Entities;

public class MaintenanceBooking : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? MotorcycleModel { get; set; }
    public string? LicensePlate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public DateTime PreferredDate { get; set; }
    public string? PreferredTime { get; set; }
    public string? Notes { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }
}
