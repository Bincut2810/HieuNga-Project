using HieuNga.Application.DTOs;

namespace HieuNga.Application.Interfaces;

public interface IBookingService
{
    Task<Guid> CreateTestRideBookingAsync(CreateBookingDto dto, CancellationToken ct = default);
    Task<Guid> CreateMaintenanceBookingAsync(CreateMaintenanceBookingDto dto, CancellationToken ct = default);
    Task<Guid> CreateConsultationAsync(CreateConsultationDto dto, CancellationToken ct = default);
}
