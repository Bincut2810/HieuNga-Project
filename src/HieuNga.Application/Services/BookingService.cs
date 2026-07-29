using HieuNga.Application;
using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class BookingService(
    IRepository<Booking> bookingRepo,
    IRepository<MaintenanceBooking> maintenanceRepo,
    IUnitOfWork unitOfWork) : IBookingService
{
    public async Task<Guid> CreateTestRideBookingAsync(CreateBookingDto dto, CancellationToken ct = default)
    {
        var booking = new Booking
        {
            Type = BookingType.TestRide,
            CustomerName = dto.CustomerName.Trim(),
            Phone = dto.Phone.Trim(),
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
            CustomerName = dto.CustomerName.Trim(),
            Phone = dto.Phone.Trim(),
            MotorcycleModel = dto.MotorcycleModel.Trim(),
            ServiceType = dto.ServiceType.Trim(),
            PreferredDate = dto.PreferredDate.Date,
            PreferredTime = dto.PreferredTime.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            Status = BookingStatus.Pending
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
            Notes = LeadAttribution.BuildNotes(
                dto.LeadSource,
                dto.Intent,
                dto.XeSlug,
                dto.ServiceSlug,
                dto.Subject,
                dto.Message),
            BranchId = dto.BranchId,
            MotorcycleId = dto.MotorcycleId
        };

        await bookingRepo.AddAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return booking.Id;
    }

    public async Task<MaintenanceBoardDto> GetMaintenanceBoardAsync(
        string? range,
        string? search,
        CancellationToken ct = default)
    {
        var all = await maintenanceRepo.FindAsync(b => !b.IsDeleted, ct);
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var weekEnd = today.AddDays(7);

        var filtered = all.AsEnumerable();

        filtered = (range ?? "today").ToLowerInvariant() switch
        {
            "tomorrow" => filtered.Where(b => b.PreferredDate.Date == tomorrow),
            "week" => filtered.Where(b => b.PreferredDate.Date >= today && b.PreferredDate.Date < weekEnd),
            "all" => filtered,
            _ => filtered.Where(b => b.PreferredDate.Date == today)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            filtered = filtered.Where(b =>
                b.CustomerName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.Phone.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.MotorcycleModel.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.ServiceType.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var items = filtered
            .OrderBy(b => b.PreferredDate)
            .ThenBy(b => b.PreferredTime)
            .ThenByDescending(b => b.CreatedAt)
            .Select(Map)
            .ToList();

        var counts = new MaintenanceBoardCounts(
            all.Count(b => b.PreferredDate.Date == today && b.Status != BookingStatus.Cancelled),
            all.Count(b => b.Status == BookingStatus.Pending),
            all.Count(b => b.PreferredDate.Date == today && b.Status == BookingStatus.Completed),
            all.Count(b => b.Status == BookingStatus.Cancelled));

        return new MaintenanceBoardDto(items, counts);
    }

    public async Task UpdateMaintenanceStatusAsync(Guid id, BookingStatus status, CancellationToken ct = default)
    {
        var entity = await maintenanceRepo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted)
            throw new InvalidOperationException("Không tìm thấy lịch hẹn.");

        if (!CanTransition(entity.Status, status))
            throw new InvalidOperationException("Không thể chuyển trạng thái lịch hẹn.");

        entity.Status = status;
        entity.UpdatedAt = DateTime.UtcNow;
        await maintenanceRepo.UpdateAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static bool CanTransition(BookingStatus from, BookingStatus to) => (from, to) switch
    {
        (BookingStatus.Pending, BookingStatus.Confirmed) => true,
        (BookingStatus.Pending, BookingStatus.Completed) => true,
        (BookingStatus.Pending, BookingStatus.Cancelled) => true,
        (BookingStatus.Confirmed, BookingStatus.Completed) => true,
        (BookingStatus.Confirmed, BookingStatus.Cancelled) => true,
        _ => false
    };

    private static MaintenanceBookingDto Map(MaintenanceBooking b) => new(
        b.Id,
        b.CustomerName,
        b.Phone,
        b.MotorcycleModel,
        b.ServiceType,
        b.PreferredDate,
        b.PreferredTime,
        b.Notes,
        b.Status,
        b.CreatedAt);
}
