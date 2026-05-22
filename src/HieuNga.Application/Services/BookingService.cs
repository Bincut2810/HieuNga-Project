using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class BookingService(IRepository<Booking> bookingRepo, IRepository<MaintenanceBooking> maintenanceRepo, IUnitOfWork unitOfWork) : IBookingService
{
    public async Task<Guid> CreateTestRideBookingAsync(CreateBookingDto dto, CancellationToken ct = default)
    {
        var booking = new Booking
        {
            Type = BookingType.TestRide,
            CustomerName = dto.CustomerName,
            Phone = dto.Phone,
            Email = dto.Email,
            PreferredDate = dto.PreferredDate,
            PreferredTime = dto.PreferredTime,
            Notes = dto.Notes,
            MotorcycleId = dto.MotorcycleId,
            BranchId = dto.BranchId
        };

        await bookingRepo.AddAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return booking.Id;
    }

    public async Task<Guid> CreateMaintenanceBookingAsync(CreateMaintenanceBookingDto dto, CancellationToken ct = default)
    {
        var booking = new MaintenanceBooking
        {
            CustomerName = dto.CustomerName,
            Phone = dto.Phone,
            Email = dto.Email,
            MotorcycleModel = dto.MotorcycleModel,
            LicensePlate = dto.LicensePlate,
            ServiceType = dto.ServiceType,
            PreferredDate = dto.PreferredDate,
            PreferredTime = dto.PreferredTime,
            Notes = dto.Notes,
            BranchId = dto.BranchId
        };

        await maintenanceRepo.AddAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return booking.Id;
    }

    public async Task<Guid> CreateConsultationAsync(CreateConsultationDto dto, CancellationToken ct = default)
    {
        var booking = new Booking
        {
            Type = BookingType.Consultation,
            CustomerName = dto.CustomerName,
            Phone = dto.Phone,
            Email = dto.Email,
            PreferredDate = DateTime.Today.AddDays(1),
            Notes = $"[{dto.Subject}] {dto.Message}",
            BranchId = dto.BranchId
        };

        await bookingRepo.AddAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return booking.Id;
    }
}
