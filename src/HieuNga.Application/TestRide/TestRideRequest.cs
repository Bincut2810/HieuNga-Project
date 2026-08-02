namespace HieuNga.Application.TestRide;

/// <summary>Create-booking request.</summary>
public sealed record TestRideRequest(
    string CustomerName,
    string PhoneNumber,
    Guid? MotorcycleId,
    DateTime AppointmentDate,
    string AppointmentTime,
    string? Source,
    string? Notes,
    Guid? BranchId = null);
