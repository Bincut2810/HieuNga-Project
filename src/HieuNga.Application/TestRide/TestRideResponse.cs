namespace HieuNga.Application.TestRide;

/// <summary>Create-booking response.</summary>
public sealed record TestRideResponse(
    Guid BookingId,
    bool Success,
    bool IsDuplicate,
    string Message,
    string CustomerName,
    string MotorcycleName,
    string AppointmentDate,
    string AppointmentTime,
    string? MotorcycleUrl,
    IReadOnlyDictionary<string, string[]>? Errors = null);
