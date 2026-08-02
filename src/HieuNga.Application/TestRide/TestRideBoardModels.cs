using HieuNga.Domain.Enums;

namespace HieuNga.Application.TestRide;

public sealed record TestRideMotorcycleOption(Guid Id, string Name, string Slug);

public sealed record TestRideAppointmentItem(
    Guid Id,
    string CustomerName,
    string PhoneNumber,
    string MotorcycleName,
    Guid? MotorcycleId,
    DateTime AppointmentDate,
    string AppointmentTime,
    string? CustomerNotes,
    string? AdminNotes,
    BookingStatus Status,
    DateTime CreatedAt);

public sealed record TestRideBoardResult(
    IReadOnlyList<TestRideAppointmentItem> Items,
    int TodayCount,
    int TomorrowCount,
    int AllCount);
