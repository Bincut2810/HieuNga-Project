using HieuNga.Application.Bookings;
using HieuNga.Application.DTOs;
using HieuNga.Domain.Enums;

namespace HieuNga.Application.Interfaces;

public interface IBookingService
{
    Task<Guid> CreateMaintenanceBookingAsync(CreateMaintenanceBookingDto dto, CancellationToken ct = default);

    /// <summary>SQL-first maintenance board query.</summary>
    Task<MaintenanceBoardDto> GetMaintenanceBoardAsync(
        BookingQuery query,
        CancellationToken ct = default);

    /// <summary>Legacy string range/search — wraps <see cref="BookingQuery"/>.</summary>
    Task<MaintenanceBoardDto> GetMaintenanceBoardAsync(
        string? range,
        string? search,
        CancellationToken ct = default);

    Task<MaintenanceBookingDto?> GetMaintenanceByIdAsync(Guid id, CancellationToken ct = default);

    Task UpdateMaintenanceStatusAsync(Guid id, BookingStatus status, CancellationToken ct = default);
}
