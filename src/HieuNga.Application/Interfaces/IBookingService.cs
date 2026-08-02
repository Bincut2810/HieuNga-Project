using HieuNga.Application.DTOs;
using HieuNga.Domain.Enums;

namespace HieuNga.Application.Interfaces;

public interface IBookingService
{
    Task<Guid> CreateMaintenanceBookingAsync(CreateMaintenanceBookingDto dto, CancellationToken ct = default);
    Task<Guid> CreateConsultationAsync(CreateConsultationDto dto, CancellationToken ct = default);

    Task<MaintenanceBoardDto> GetMaintenanceBoardAsync(
        string? range,
        string? search,
        CancellationToken ct = default);

    Task UpdateMaintenanceStatusAsync(Guid id, BookingStatus status, CancellationToken ct = default);
}
