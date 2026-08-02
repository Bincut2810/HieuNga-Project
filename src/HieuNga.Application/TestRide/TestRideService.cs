using FluentValidation;
using HieuNga.Application;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.TestRide;

/// <summary>Test Ride booking — persists to <c>bookings</c> with <see cref="BookingType.TestRide"/>.</summary>
public sealed class TestRideService(
    IRepository<Booking> bookingRepo,
    IMotorcycleRepository motorcycleRepo,
    IBranchRepository branchRepo,
    IUnitOfWork unitOfWork,
    ITestRideCreateSynchronizer createSynchronizer,
    IValidator<TestRideRequest> validator) : ITestRideService
{
    public async Task<IReadOnlyList<TestRideMotorcycleOption>> GetMotorcycleOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await motorcycleRepo.FindAsync(
            m => !m.IsDeleted && m.IsPublished,
            cancellationToken);
        return list
            .OrderBy(m => m.Name)
            .Select(m => new TestRideMotorcycleOption(m.Id, m.Name, m.Slug))
            .ToList();
    }

    public async Task<TestRideResponse> CreateAsync(
        TestRideRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return FailValidation(request, errors);
        }

        if (request.MotorcycleId is not Guid motorcycleId || motorcycleId == Guid.Empty)
            return FailMotorcycle(request);

        var moto = await motorcycleRepo.GetByIdAsync(motorcycleId, cancellationToken);
        if (moto is null || moto.IsDeleted || !moto.IsPublished)
            return FailMotorcycle(request);

        var phone = TestRidePhoneNormalizer.Normalize(request.PhoneNumber);
        var phoneVariants = TestRidePhoneNormalizer.LookupVariants(phone);
        // Calendar Y-M-D for lock key / labels; UTC bounds for timestamptz (Npgsql requires Kind=Utc).
        var appointmentCalendar = request.AppointmentDate;
        var dayUtc = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(appointmentCalendar);
        var dayEndUtc = TestRideVietnamTime.ConvertLocalAppointmentDateEndExclusiveToUtc(appointmentCalendar);
        var since = TestRideVietnamTime.UtcNow.AddMinutes(-30);
        var motorcycleName = moto.Name;
        var motorcycleUrl = $"/xe/{moto.Slug}";
        var dateLabel = new DateTime(
            appointmentCalendar.Year,
            appointmentCalendar.Month,
            appointmentCalendar.Day).ToString("dd/MM/yyyy");
        var timeLabel = request.AppointmentTime.Trim();
        var lockKey =
            $"{phone}|{motorcycleId:D}|{appointmentCalendar.Year:D4}-{appointmentCalendar.Month:D2}-{appointmentCalendar.Day:D2}";

        return await createSynchronizer.ExecuteAsync(lockKey, async ct =>
        {
            var recent = await bookingRepo.FindAsync(
                b => !b.IsDeleted
                     && b.Type == BookingType.TestRide
                     && b.Status != BookingStatus.Cancelled
                     && phoneVariants.Contains(b.Phone)
                     && b.PreferredDate >= dayUtc
                     && b.PreferredDate < dayEndUtc
                     && b.MotorcycleId == motorcycleId
                     && b.CreatedAt >= since,
                ct);

            var existing = recent.OrderByDescending(b => b.CreatedAt).FirstOrDefault();
            if (existing is not null)
            {
                return new TestRideResponse(
                    existing.Id,
                    Success: true,
                    IsDuplicate: true,
                    Message: "Bạn đã gửi lịch hẹn trước đó. Nhân viên sẽ sớm liên hệ với bạn.",
                    CustomerName: existing.CustomerName,
                    MotorcycleName: motorcycleName,
                    AppointmentDate: dateLabel,
                    AppointmentTime: existing.PreferredTime ?? timeLabel,
                    MotorcycleUrl: motorcycleUrl);
            }

            var branches = await branchRepo.FindAsync(b => !b.IsDeleted && b.IsActive, ct);
            Guid? branchId = null;
            if (request.BranchId is Guid requestedBranch
                && branches.Any(b => b.Id == requestedBranch))
            {
                branchId = requestedBranch;
            }
            else
            {
                branchId = branches.FirstOrDefault(b => b.IsHeadOffice)?.Id
                    ?? branches.FirstOrDefault()?.Id;
            }

            var notes = LeadAttribution.BuildNotes(
                request.Source,
                "lai-thu",
                moto.Slug,
                null,
                null,
                request.Notes,
                $"bike={moto.Name}");

            var booking = new Booking
            {
                Type = BookingType.TestRide,
                CustomerName = request.CustomerName.Trim(),
                Phone = phone,
                PreferredDate = dayUtc,
                PreferredTime = timeLabel,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                MotorcycleId = motorcycleId,
                BranchId = branchId,
                Status = BookingStatus.Pending
            };

            await bookingRepo.AddAsync(booking, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return new TestRideResponse(
                booking.Id,
                Success: true,
                IsDuplicate: false,
                Message: "Đặt lịch thành công",
                CustomerName: booking.CustomerName,
                MotorcycleName: motorcycleName,
                AppointmentDate: dateLabel,
                AppointmentTime: timeLabel,
                MotorcycleUrl: motorcycleUrl);
        }, cancellationToken);
    }

    public async Task<TestRideBoardResult> GetBoardAsync(
        string range,
        string? search,
        CancellationToken cancellationToken = default)
    {
        // Vietnam business days → UTC day bounds (single conversion helper; Kind=Utc for Npgsql).
        var todayVn = TestRideVietnamTime.Today;
        var todayUtc = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn);
        var tomorrowUtc = TestRideVietnamTime.ConvertLocalAppointmentDateEndExclusiveToUtc(todayVn);
        var dayAfterTomorrowUtc = TestRideVietnamTime.ConvertLocalAppointmentDateToUtc(todayVn.AddDays(2));

        var todayCount = await bookingRepo.CountAsync(
            b => !b.IsDeleted
                 && b.Type == BookingType.TestRide
                 && b.Status != BookingStatus.Cancelled
                 && b.PreferredDate >= todayUtc
                 && b.PreferredDate < tomorrowUtc,
            cancellationToken);

        var tomorrowCount = await bookingRepo.CountAsync(
            b => !b.IsDeleted
                 && b.Type == BookingType.TestRide
                 && b.Status != BookingStatus.Cancelled
                 && b.PreferredDate >= tomorrowUtc
                 && b.PreferredDate < dayAfterTomorrowUtc,
            cancellationToken);

        var allCount = await bookingRepo.CountAsync(
            b => !b.IsDeleted
                 && b.Type == BookingType.TestRide
                 && b.Status != BookingStatus.Cancelled,
            cancellationToken);

        var q = search?.Trim();
        List<Guid>? matchingMotoIds = null;
        if (!string.IsNullOrEmpty(q))
        {
            var motos = await motorcycleRepo.FindAsync(
                m => m.Name.Contains(q),
                cancellationToken);
            matchingMotoIds = motos.Select(m => m.Id).ToList();
        }

        var normalizedRange = (range ?? "today").ToLowerInvariant();
        var rows = await bookingRepo.FindAsync(
            BuildBoardPredicate(normalizedRange, todayUtc, tomorrowUtc, dayAfterTomorrowUtc, q, matchingMotoIds),
            cancellationToken);

        var motoIds = rows.Where(b => b.MotorcycleId.HasValue).Select(b => b.MotorcycleId!.Value).Distinct().ToList();
        var motoNames = new Dictionary<Guid, string>();
        if (motoIds.Count > 0)
        {
            var motos = await motorcycleRepo.FindAsync(m => motoIds.Contains(m.Id), cancellationToken);
            foreach (var m in motos)
                motoNames[m.Id] = m.Name;
        }

        string MotoName(Booking b) =>
            b.MotorcycleId is Guid id && motoNames.TryGetValue(id, out var n) ? n : "Chưa chọn xe";

        var items = rows
            .OrderBy(b => b.PreferredDate)
            .ThenBy(b => b.PreferredTime ?? "")
            .ThenBy(b => b.CreatedAt)
            .Select(b => MapItem(b, MotoName(b)))
            .ToList();

        return new TestRideBoardResult(items, todayCount, tomorrowCount, allCount);
    }

    public async Task<TestRideAppointmentItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var b = await bookingRepo.GetByIdAsync(id, cancellationToken);
        if (b is null || b.IsDeleted || b.Type != BookingType.TestRide)
            return null;

        string motoName = "Chưa chọn xe";
        if (b.MotorcycleId is Guid mid)
        {
            var moto = await motorcycleRepo.GetByIdAsync(mid, cancellationToken);
            if (moto is not null) motoName = moto.Name;
        }

        return MapItem(b, motoName);
    }

    public async Task UpdateStatusAsync(
        Guid id,
        BookingStatus status,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityAsync(id, cancellationToken);
        ApplyStatus(entity, status);
        entity.UpdatedAt = TestRideVietnamTime.UtcNow;
        await bookingRepo.UpdateAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAdminAsync(
        Guid id,
        BookingStatus status,
        string? adminNotes,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityAsync(id, cancellationToken);
        ApplyStatus(entity, status);
        entity.AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        entity.UpdatedAt = TestRideVietnamTime.UtcNow;
        await bookingRepo.UpdateAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<Booking, bool>> BuildBoardPredicate(
        string range,
        DateTime todayUtc,
        DateTime tomorrowUtc,
        DateTime dayAfterTomorrowUtc,
        string? q,
        List<Guid>? matchingMotoIds)
    {
        var hasSearch = !string.IsNullOrEmpty(q);
        var motoIds = matchingMotoIds ?? [];

        return range switch
        {
            "tomorrow" when hasSearch => b =>
                !b.IsDeleted
                && b.Type == BookingType.TestRide
                && b.PreferredDate >= tomorrowUtc
                && b.PreferredDate < dayAfterTomorrowUtc
                && (b.CustomerName.Contains(q!)
                    || b.Phone.Contains(q!)
                    || (b.Notes != null && b.Notes.Contains(q!))
                    || (b.MotorcycleId.HasValue && motoIds.Contains(b.MotorcycleId.Value))),
            "tomorrow" => b =>
                !b.IsDeleted
                && b.Type == BookingType.TestRide
                && b.PreferredDate >= tomorrowUtc
                && b.PreferredDate < dayAfterTomorrowUtc,
            "all" when hasSearch => b =>
                !b.IsDeleted
                && b.Type == BookingType.TestRide
                && (b.CustomerName.Contains(q!)
                    || b.Phone.Contains(q!)
                    || (b.Notes != null && b.Notes.Contains(q!))
                    || (b.MotorcycleId.HasValue && motoIds.Contains(b.MotorcycleId.Value))),
            "all" => b =>
                !b.IsDeleted
                && b.Type == BookingType.TestRide,
            _ when hasSearch => b =>
                !b.IsDeleted
                && b.Type == BookingType.TestRide
                && b.PreferredDate >= todayUtc
                && b.PreferredDate < tomorrowUtc
                && (b.CustomerName.Contains(q!)
                    || b.Phone.Contains(q!)
                    || (b.Notes != null && b.Notes.Contains(q!))
                    || (b.MotorcycleId.HasValue && motoIds.Contains(b.MotorcycleId.Value))),
            _ => b =>
                !b.IsDeleted
                && b.Type == BookingType.TestRide
                && b.PreferredDate >= todayUtc
                && b.PreferredDate < tomorrowUtc
        };
    }

    private async Task<Booking> GetEntityAsync(Guid id, CancellationToken ct)
    {
        var entity = await bookingRepo.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted || entity.Type != BookingType.TestRide)
            throw new InvalidOperationException("Không tìm thấy lịch xem xe.");
        return entity;
    }

    private static void ApplyStatus(Booking entity, BookingStatus status)
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

    private static TestRideAppointmentItem MapItem(Booking b, string motorcycleName) => new(
        b.Id,
        b.CustomerName,
        b.Phone,
        motorcycleName,
        b.MotorcycleId,
        b.PreferredDate,
        b.PreferredTime ?? "",
        LeadAttribution.StripTag(b.Notes),
        b.AdminNotes,
        b.Status,
        b.CreatedAt);

    private static TestRideResponse FailValidation(
        TestRideRequest request,
        IReadOnlyDictionary<string, string[]> errors) =>
        new(
            Guid.Empty,
            Success: false,
            IsDuplicate: false,
            Message: "Vui lòng kiểm tra lại thông tin.",
            CustomerName: request.CustomerName ?? "",
            MotorcycleName: "",
            AppointmentDate: "",
            AppointmentTime: request.AppointmentTime ?? "",
            MotorcycleUrl: null,
            Errors: errors);

    private static TestRideResponse FailMotorcycle(TestRideRequest request) =>
        FailValidation(request, new Dictionary<string, string[]>
        {
            [nameof(TestRideRequest.MotorcycleId)] =
                ["Xe không hợp lệ hoặc không còn được đăng bán."]
        });
}
