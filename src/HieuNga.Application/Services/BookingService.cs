using System.Linq.Expressions;
using HieuNga.Application;
using HieuNga.Application.Bookings;
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
            PreferredTime = TestRideValidator.NormalizeAppointmentTime(dto.PreferredTime),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            Status = BookingStatus.Pending
        };

        await maintenanceRepo.AddAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return booking.Id;
    }

    public Task<MaintenanceBoardDto> GetMaintenanceBoardAsync(
        string? range,
        string? search,
        CancellationToken ct = default) =>
        GetMaintenanceBoardAsync(
            BookingQuery.FromAdmin(range, search),
            includeLegacyCounts: true,
            ct);

    public Task<MaintenanceBoardDto> GetMaintenanceBoardAsync(
        BookingQuery query,
        CancellationToken ct = default) =>
        GetMaintenanceBoardAsync(query, includeLegacyCounts: false, ct);

    private async Task<MaintenanceBoardDto> GetMaintenanceBoardAsync(
        BookingQuery query,
        bool includeLegacyCounts,
        CancellationToken ct)
    {
        var bounds = BookingDateBounds.ForVietnamToday();
        var predicate = BuildBoardPredicate(query, bounds);
        var rows = await maintenanceRepo.FindAsync(
            predicate,
            ordered => ordered
                .OrderBy(b => b.PreferredDate)
                .ThenBy(b => b.PreferredTime)
                .ThenByDescending(b => b.CreatedAt),
            query.Skip > 0 ? query.Skip : null,
            query.Take,
            ct);

        var items = rows.Select(MapMaintenance).ToList();

        if (!includeLegacyCounts)
            return new MaintenanceBoardDto(items, new MaintenanceBoardCounts(0, 0, 0, 0));

        var today = await maintenanceRepo.CountAsync(
            b => !b.IsDeleted
                 && b.PreferredDate >= bounds.TodayUtc
                 && b.PreferredDate < bounds.TomorrowUtc
                 && b.Status != BookingStatus.Cancelled,
            ct);
        var waiting = await maintenanceRepo.CountAsync(
            b => !b.IsDeleted && b.Status == BookingStatus.Pending,
            ct);
        var completedToday = await maintenanceRepo.CountAsync(
            b => !b.IsDeleted
                 && b.PreferredDate >= bounds.TodayUtc
                 && b.PreferredDate < bounds.TomorrowUtc
                 && b.Status == BookingStatus.Completed,
            ct);
        var cancelled = await maintenanceRepo.CountAsync(
            b => !b.IsDeleted && b.Status == BookingStatus.Cancelled,
            ct);

        return new MaintenanceBoardDto(items, new MaintenanceBoardCounts(today, waiting, completedToday, cancelled));
    }

    public async Task<MaintenanceBookingDto?> GetMaintenanceByIdAsync(Guid id, CancellationToken ct = default)
    {
        var b = await maintenanceRepo.GetByIdAsync(id, ct);
        if (b is null || b.IsDeleted)
            return null;
        return MapMaintenance(b);
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

    private static Expression<Func<MaintenanceBooking, bool>> BuildBoardPredicate(
        BookingQuery query,
        BookingDateBounds bounds)
    {
        var q = query.NormalizedSearch;
        var hasSearch = q is not null;
        var range = query.NormalizedRange;
        var status = query.Status;
        var todayUtc = bounds.TodayUtc;
        var tomorrowUtc = bounds.TomorrowUtc;
        var dayAfterTomorrowUtc = bounds.DayAfterTomorrowUtc;
        var weekEndUtc = bounds.WeekEndUtc;

        if (status is BookingStatus.Completed)
        {
            return hasSearch
                ? b => !b.IsDeleted && b.Status == BookingStatus.Completed
                       && (b.CustomerName.Contains(q!)
                           || b.Phone.Contains(q!)
                           || b.MotorcycleModel.Contains(q!)
                           || b.ServiceType.Contains(q!)
                           || (b.Notes != null && b.Notes.Contains(q!)))
                : b => !b.IsDeleted && b.Status == BookingStatus.Completed;
        }

        if (status is BookingStatus.Cancelled)
        {
            return hasSearch
                ? b => !b.IsDeleted && b.Status == BookingStatus.Cancelled
                       && (b.CustomerName.Contains(q!)
                           || b.Phone.Contains(q!)
                           || b.MotorcycleModel.Contains(q!)
                           || b.ServiceType.Contains(q!)
                           || (b.Notes != null && b.Notes.Contains(q!)))
                : b => !b.IsDeleted && b.Status == BookingStatus.Cancelled;
        }

        if (range == "late")
        {
            return hasSearch
                ? b => !b.IsDeleted
                       && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                       && b.PreferredDate < tomorrowUtc
                       && (b.CustomerName.Contains(q!)
                           || b.Phone.Contains(q!)
                           || b.MotorcycleModel.Contains(q!)
                           || b.ServiceType.Contains(q!)
                           || (b.Notes != null && b.Notes.Contains(q!)))
                : b => !b.IsDeleted
                       && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                       && b.PreferredDate < tomorrowUtc;
        }

        return range switch
        {
            "tomorrow" when hasSearch => b =>
                !b.IsDeleted
                && b.PreferredDate >= tomorrowUtc
                && b.PreferredDate < dayAfterTomorrowUtc
                && (b.CustomerName.Contains(q!)
                    || b.Phone.Contains(q!)
                    || b.MotorcycleModel.Contains(q!)
                    || b.ServiceType.Contains(q!)
                    || (b.Notes != null && b.Notes.Contains(q!))),
            "tomorrow" => b =>
                !b.IsDeleted
                && b.PreferredDate >= tomorrowUtc
                && b.PreferredDate < dayAfterTomorrowUtc,
            "week" when hasSearch => b =>
                !b.IsDeleted
                && b.PreferredDate >= todayUtc
                && b.PreferredDate < weekEndUtc
                && (b.CustomerName.Contains(q!)
                    || b.Phone.Contains(q!)
                    || b.MotorcycleModel.Contains(q!)
                    || b.ServiceType.Contains(q!)
                    || (b.Notes != null && b.Notes.Contains(q!))),
            "week" => b =>
                !b.IsDeleted
                && b.PreferredDate >= todayUtc
                && b.PreferredDate < weekEndUtc,
            "all" when hasSearch => b =>
                !b.IsDeleted
                && (b.CustomerName.Contains(q!)
                    || b.Phone.Contains(q!)
                    || b.MotorcycleModel.Contains(q!)
                    || b.ServiceType.Contains(q!)
                    || (b.Notes != null && b.Notes.Contains(q!))),
            "all" => b => !b.IsDeleted,
            _ when hasSearch => b =>
                !b.IsDeleted
                && b.PreferredDate >= todayUtc
                && b.PreferredDate < tomorrowUtc
                && (b.CustomerName.Contains(q!)
                    || b.Phone.Contains(q!)
                    || b.MotorcycleModel.Contains(q!)
                    || b.ServiceType.Contains(q!)
                    || (b.Notes != null && b.Notes.Contains(q!))),
            _ => b =>
                !b.IsDeleted
                && b.PreferredDate >= todayUtc
                && b.PreferredDate < tomorrowUtc
        };
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
