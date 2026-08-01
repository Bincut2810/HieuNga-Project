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
    IMotorcycleRepository motorcycleRepo,
    IBranchRepository branchRepo,
    IUnitOfWork unitOfWork) : IBookingService
{
    public async Task<Guid> CreateTestRideBookingAsync(CreateBookingDto dto, CancellationToken ct = default)
    {
        var booking = new Booking
        {
            Type = BookingType.TestRide,
            CustomerName = dto.CustomerName.Trim(),
            Phone = dto.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            PreferredDate = dto.PreferredDate.Date,
            PreferredTime = dto.PreferredTime?.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            MotorcycleId = dto.MotorcycleId,
            BranchId = dto.BranchId,
            Status = BookingStatus.Pending
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
            .Select(MapMaintenance)
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

    public async Task<TestRideBoardDto> GetTestRideBoardAsync(
        string? range,
        string? search,
        CancellationToken ct = default)
    {
        var all = await bookingRepo.FindAsync(
            b => !b.IsDeleted && b.Type == BookingType.TestRide,
            ct);

        var motoIds = all.Where(b => b.MotorcycleId.HasValue).Select(b => b.MotorcycleId!.Value).Distinct().ToList();
        var branchIds = all.Where(b => b.BranchId.HasValue).Select(b => b.BranchId!.Value).Distinct().ToList();

        var motoNames = new Dictionary<Guid, string>();
        if (motoIds.Count > 0)
        {
            var motos = await motorcycleRepo.FindAsync(m => motoIds.Contains(m.Id), ct);
            foreach (var m in motos)
                motoNames[m.Id] = m.Name;
        }

        var branchNames = new Dictionary<Guid, string>();
        if (branchIds.Count > 0)
        {
            var branches = await branchRepo.FindAsync(b => branchIds.Contains(b.Id), ct);
            foreach (var b in branches)
                branchNames[b.Id] = b.Name;
        }

        string? MotoName(Booking b) =>
            b.MotorcycleId is Guid mid && motoNames.TryGetValue(mid, out var n) ? n : null;
        string? BranchName(Booking b) =>
            b.BranchId is Guid bid && branchNames.TryGetValue(bid, out var n) ? n : null;

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        IEnumerable<Booking> filtered = all;

        // Board loads "all" once; client tabs filter Today/Tomorrow/All.
        filtered = (range ?? "all").ToLowerInvariant() switch
        {
            "today" => filtered.Where(b => b.PreferredDate.Date == today),
            "tomorrow" => filtered.Where(b => b.PreferredDate.Date == tomorrow),
            _ => filtered
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            filtered = filtered.Where(b =>
                b.CustomerName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.Phone.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (MotoName(b)?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (BranchName(b)?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (LeadAttribution.StripTag(b.Notes)?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var items = filtered
            .OrderBy(b => b.PreferredDate.Date)
            .ThenBy(b => b.PreferredTime ?? "")
            .ThenBy(b => b.CreatedAt)
            .Select(b => MapTestRide(b, MotoName(b), BranchName(b)))
            .ToList();

        static bool Active(Booking b) => b.Status != BookingStatus.Cancelled;
        var todayRows = all.Where(b => b.PreferredDate.Date == today).ToList();
        var counts = new TestRideBoardCounts(
            TodayTotal: todayRows.Count(Active),
            TomorrowTotal: all.Count(b => b.PreferredDate.Date == tomorrow && Active(b)),
            AllTotal: all.Count(Active),
            TodayWaiting: todayRows.Count(b => b.Status == BookingStatus.Pending),
            TodayConfirmed: todayRows.Count(b => b.Status == BookingStatus.Confirmed),
            TodayCompleted: todayRows.Count(b => b.Status == BookingStatus.Completed));

        return new TestRideBoardDto(items, counts);
    }

    public async Task UpdateTestRideStatusAsync(Guid id, BookingStatus status, CancellationToken ct = default)
    {
        var entity = await GetTestRideEntityAsync(id, ct);
        ApplyTestRideStatus(entity, status);
        entity.UpdatedAt = DateTime.UtcNow;
        await bookingRepo.UpdateAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task UpdateTestRideAdminAsync(
        Guid id,
        BookingStatus status,
        string? adminNotes,
        CancellationToken ct = default)
    {
        var entity = await GetTestRideEntityAsync(id, ct);
        ApplyTestRideStatus(entity, status);
        entity.AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await bookingRepo.UpdateAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Booking> GetTestRideEntityAsync(Guid id, CancellationToken ct)
    {
        var entity = await bookingRepo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted || entity.Type != BookingType.TestRide)
            throw new InvalidOperationException("Không tìm thấy lịch xem xe.");
        return entity;
    }

    private static void ApplyTestRideStatus(Booking entity, BookingStatus status)
    {
        if (entity.Status == status) return;
        if (!CanTransition(entity.Status, status))
            throw new InvalidOperationException("Không thể chuyển trạng thái lịch hẹn.");
        entity.Status = status;
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

    private static TestRideBookingDto MapTestRide(Booking b, string? motorcycleName, string? branchName) => new(
        b.Id,
        b.CustomerName,
        b.Phone,
        motorcycleName,
        b.MotorcycleId,
        branchName,
        b.PreferredDate,
        b.PreferredTime ?? "",
        LeadAttribution.StripTag(b.Notes),
        b.Status,
        b.CreatedAt);
}
