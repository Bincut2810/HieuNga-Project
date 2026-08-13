using HieuNga.Application.Bookings;
using HieuNga.Domain.Enums;

namespace HieuNga.Application.TestRide;

/// <summary>Test Ride booking application port.</summary>
public interface ITestRideService
{
    Task<IReadOnlyList<TestRideMotorcycleOption>> GetMotorcycleOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<TestRideResponse> CreateAsync(
        TestRideRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>SQL-first board query. Prefer this overload for admin surfaces.</summary>
    Task<TestRideBoardResult> GetBoardAsync(
        BookingQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Legacy string range/search — wraps <see cref="BookingQuery"/>.</summary>
    Task<TestRideBoardResult> GetBoardAsync(
        string range,
        string? search,
        CancellationToken cancellationToken = default);

    Task<TestRideAppointmentItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        Guid id,
        BookingStatus status,
        CancellationToken cancellationToken = default);

    Task UpdateAdminAsync(
        Guid id,
        BookingStatus status,
        string? adminNotes,
        CancellationToken cancellationToken = default);
}
