using HieuNga.Application;
using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.TestRide;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class BookingService(
    IRepository<MaintenanceBooking> maintenanceRepo,
    IUnitOfWork unitOfWork) : IBookingService
{
    public async Task<Guid> CreateMaintenanceBookingAsync(CreateMaintenanceBookingDto dto, CancellationToken ct = default)
    {
        var phone = TestRidePhoneNormalizer.Normalize(dto.Phone);
        var dayUtc = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(dto.PreferredDate);

        var booking = new MaintenanceBooking
        {
            CustomerName = dto.CustomerName.Trim(),
            Phone = phone,
            MotorcycleModel = dto.MotorcycleModel.Trim(),
            ServiceType = dto.ServiceType.Trim(),
            PreferredDate = dayUtc,
            PreferredTime = dto.PreferredTime.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            Status = BookingStatus.Pending
        };

        await maintenanceRepo.AddAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return booking.Id;
    }

    public async Task<MaintenanceBoardDto> GetMaintenanceBoardAsync(
        string? range,
        string? search,
        CancellationToken ct = default)
    {
        var todayVn = TestRideVietnamTime.Today;
        var todayUtc = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn);
        var tomorrowUtc = TestRideVietnamTime.ConvertLocalAppointmentDateEndExclusiveToUtc(todayVn);
        var weekEndUtc = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn.AddDays(7));

        var all = await maintenanceRepo.FindAsync(b => !b.IsDeleted, ct);

        var filtered = (range ?? "today").ToLowerInvariant() switch
        {
            "tomorrow" => all.Where(b => b.PreferredDate >= tomorrowUtc
                && b.PreferredDate < TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn.AddDays(2))),
            "week" => all.Where(b => b.PreferredDate >= todayUtc && b.PreferredDate < weekEndUtc),
            "all" => all.AsEnumerable(),
            _ => all.Where(b => b.PreferredDate >= todayUtc && b.PreferredDate < tomorrowUtc)
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
            .Select(MapMaintenance)
            .ToList();

        var counts = new MaintenanceBoardCounts(
            all.Count(b => b.PreferredDate >= todayUtc && b.PreferredDate < tomorrowUtc
                && b.Status != BookingStatus.Cancelled),
            all.Count(b => b.Status == BookingStatus.Pending),
            all.Count(b => b.PreferredDate >= todayUtc && b.PreferredDate < tomorrowUtc
                && b.Status == BookingStatus.Completed),
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
        entity.UpdatedAt = TestRideVietnamTime.UtcNow;
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

    private static MaintenanceBookingDto MapMaintenance(MaintenanceBooking b) => new(
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
