using HieuNga.Application.DTOs;
using HieuNga.Application.TestRide;
using HieuNga.Domain.Enums;

namespace HieuNga.Web.ViewModels.Admin.BookingCenter;

public enum BookingCenterKind
{
    TestRide,
    Maintenance
}

/// <summary>Presentation-only card/drawer model. Maps existing DTOs; no new persistence.</summary>
public sealed class BookingCenterItemVm
{
    public Guid Id { get; init; }
    public BookingCenterKind Kind { get; init; }
    public string KindLabel => Kind == BookingCenterKind.TestRide ? "Test Ride" : "Maintenance";
    public string TimeLabel { get; init; } = "—";
    public string DateLabel { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Vehicle { get; init; } = "";
    public string? Service { get; init; }
    public string? Branch { get; init; }
    public string? LeadSource { get; init; }
    public string? CustomerNotes { get; init; }
    public string? AdminNotes { get; init; }
    public BookingStatus Status { get; init; }
    public DateTime AppointmentUtc { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsLate { get; init; }
    public int SortMinutes { get; init; }

    /// <summary>Reception label for status chip (maps existing enum).</summary>
    public string StatusChipLabel => Status switch
    {
        BookingStatus.Pending => IsLate ? "Trễ giờ" : "Đang chờ",
        BookingStatus.Confirmed => IsLate ? "Trễ giờ" : "Đã đến",
        BookingStatus.Completed => "Hoàn thành",
        BookingStatus.Cancelled => "Đã hủy",
        _ => "—"
    };

    public string StatusChipClass => Status switch
    {
        BookingStatus.Pending when IsLate => "bc-chip-late",
        BookingStatus.Confirmed when IsLate => "bc-chip-late",
        BookingStatus.Pending => "bc-chip-waiting",
        BookingStatus.Confirmed => "bc-chip-arrived",
        BookingStatus.Completed => "bc-chip-done",
        BookingStatus.Cancelled => "bc-chip-cancel",
        _ => "bc-chip-muted"
    };

    public bool CanMarkArrived => Status == BookingStatus.Pending;
    public bool CanComplete => Status is BookingStatus.Pending or BookingStatus.Confirmed;
    public bool CanCancel => Status is BookingStatus.Pending or BookingStatus.Confirmed;
    public bool CanEditAdminNotes => Kind == BookingCenterKind.TestRide;
}

public sealed class BookingCenterSummaryVm
{
    public int TodayTotal { get; init; }
    public int Arrived { get; init; }
    public int Waiting { get; init; }
    public int Completed { get; init; }
    public int Cancelled { get; init; }
    public int Late { get; init; }
}

public sealed class BookingCenterTimelineGroupVm
{
    public string TimeLabel { get; init; } = "";
    public IReadOnlyList<BookingCenterItemVm> Items { get; init; } = [];
}

public static class BookingCenterMapper
{
    public static BookingCenterItemVm FromTestRide(TestRideAppointmentItem b, DateTime nowVn)
    {
        var vnDate = TestRideVietnamTime.ToVietnamDate(b.AppointmentDate);
        var minutes = ParseTimeMinutes(b.AppointmentTime);
        var isLate = IsLate(b.Status, vnDate, minutes, nowVn);
        return new BookingCenterItemVm
        {
            Id = b.Id,
            Kind = BookingCenterKind.TestRide,
            TimeLabel = string.IsNullOrWhiteSpace(b.AppointmentTime) ? "—" : b.AppointmentTime,
            DateLabel = vnDate.ToString("dd/MM/yyyy"),
            CustomerName = b.CustomerName,
            Phone = b.PhoneNumber,
            Vehicle = b.MotorcycleName,
            Service = null,
            Branch = null,
            LeadSource = null,
            CustomerNotes = b.CustomerNotes,
            AdminNotes = b.AdminNotes,
            Status = b.Status,
            AppointmentUtc = b.AppointmentDate,
            CreatedAt = b.CreatedAt,
            IsLate = isLate,
            SortMinutes = minutes
        };
    }

    public static BookingCenterItemVm FromMaintenance(MaintenanceBookingDto b, DateTime nowVn)
    {
        var vnDate = TestRideVietnamTime.ToVietnamDate(b.PreferredDate);
        var minutes = ParseTimeMinutes(b.PreferredTime);
        var isLate = IsLate(b.Status, vnDate, minutes, nowVn);
        return new BookingCenterItemVm
        {
            Id = b.Id,
            Kind = BookingCenterKind.Maintenance,
            TimeLabel = string.IsNullOrWhiteSpace(b.PreferredTime) ? "—" : b.PreferredTime,
            DateLabel = vnDate.ToString("dd/MM/yyyy"),
            CustomerName = b.CustomerName,
            Phone = b.Phone,
            Vehicle = b.MotorcycleModel,
            Service = b.ServiceType,
            Branch = null,
            LeadSource = null,
            CustomerNotes = b.Notes,
            AdminNotes = null,
            Status = b.Status,
            AppointmentUtc = b.PreferredDate,
            CreatedAt = b.CreatedAt,
            IsLate = isLate,
            SortMinutes = minutes
        };
    }

    public static BookingCenterSummaryVm BuildSummary(IEnumerable<BookingCenterItemVm> todayItems)
    {
        var list = todayItems.ToList();
        return new BookingCenterSummaryVm
        {
            TodayTotal = list.Count(i => i.Status != BookingStatus.Cancelled),
            Arrived = list.Count(i => i.Status == BookingStatus.Confirmed),
            Waiting = list.Count(i => i.Status == BookingStatus.Pending),
            Completed = list.Count(i => i.Status == BookingStatus.Completed),
            Cancelled = list.Count(i => i.Status == BookingStatus.Cancelled),
            Late = list.Count(i => i.IsLate)
        };
    }

    public static IReadOnlyList<BookingCenterTimelineGroupVm> GroupByTime(IEnumerable<BookingCenterItemVm> items) =>
        items
            .OrderBy(i => i.SortMinutes)
            .ThenBy(i => i.CustomerName, StringComparer.OrdinalIgnoreCase)
            .GroupBy(i => i.TimeLabel)
            .Select(g => new BookingCenterTimelineGroupVm
            {
                TimeLabel = g.Key,
                Items = g.ToList()
            })
            .ToList();

    public static int ParseTimeMinutes(string? time)
    {
        if (string.IsNullOrWhiteSpace(time)) return 24 * 60;
        var parts = time.Trim().Split(':');
        if (parts.Length < 2) return 24 * 60;
        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m))
            return 24 * 60;
        return h * 60 + m;
    }

    public static bool IsLate(BookingStatus status, DateTime vnDate, int sortMinutes, DateTime nowVn)
    {
        if (status is BookingStatus.Completed or BookingStatus.Cancelled) return false;
        if (sortMinutes >= 24 * 60) return false;
        var appointment = vnDate.Date.AddMinutes(sortMinutes);
        return appointment < nowVn;
    }
}
