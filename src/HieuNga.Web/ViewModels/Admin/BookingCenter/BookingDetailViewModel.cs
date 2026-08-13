using HieuNga.Application.DTOs;
using HieuNga.Application.TestRide;
using HieuNga.Domain.Enums;

namespace HieuNga.Web.ViewModels.Admin.BookingCenter;

/// <summary>
/// Single drawer detail model for all booking kinds (Test Ride, Maintenance, future types).
/// </summary>
public sealed class BookingDetailViewModel
{
    public Guid Id { get; init; }
    public string Kind { get; init; } = "";
    public string KindLabel { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string PhoneNumber { get; init; } = "";
    public string Vehicle { get; init; } = "";
    public string? Service { get; init; }
    public string? Branch { get; init; }
    public string? LeadSource { get; init; }
    public string AppointmentDate { get; init; } = "";
    public string AppointmentTime { get; init; } = "";
    public string? CustomerNotes { get; init; }
    public string? AdminNotes { get; init; }
    public BookingStatus Status { get; init; }
    public string StatusLabel { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public bool IsLate { get; init; }
    public bool CanEditAdminNotes { get; init; }

    // Future optional surfaces — keep null until those types exist.
    public string? ConsultationTopic { get; init; }
    public string? TradeInVehicle { get; init; }
    public string? FinanceProduct { get; init; }

    public static BookingDetailViewModel FromTestRide(TestRideAppointmentItem item, DateTime nowVn)
    {
        var card = BookingCenterMapper.FromTestRide(item, nowVn);
        return FromCard(card, "testride");
    }

    public static BookingDetailViewModel FromMaintenance(MaintenanceBookingDto item, DateTime nowVn)
    {
        var card = BookingCenterMapper.FromMaintenance(item, nowVn);
        return FromCard(card, "maint");
    }

    public static BookingDetailViewModel FromCard(BookingCenterItemVm card, string kindKey) => new()
    {
        Id = card.Id,
        Kind = kindKey,
        KindLabel = card.KindLabel,
        CustomerName = card.CustomerName,
        PhoneNumber = card.Phone,
        Vehicle = card.Vehicle,
        Service = card.Service,
        Branch = card.Branch ?? "—",
        LeadSource = card.LeadSource ?? "—",
        AppointmentDate = card.DateLabel,
        AppointmentTime = card.TimeLabel,
        CustomerNotes = card.CustomerNotes,
        AdminNotes = card.AdminNotes,
        Status = card.Status,
        StatusLabel = card.StatusChipLabel,
        CreatedAt = card.CreatedAt,
        IsLate = card.IsLate,
        CanEditAdminNotes = card.CanEditAdminNotes
    };
}
