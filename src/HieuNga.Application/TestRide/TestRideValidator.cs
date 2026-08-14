using System.Globalization;
using FluentValidation;

namespace HieuNga.Application.TestRide;

public sealed class TestRideValidator : AbstractValidator<TestRideRequest>
{
    /// <summary>
    /// Accepts browser <c>type="time"</c> values (HH:mm or HH:mm:ss).
    /// No invented business-hour window — only format validity.
    /// </summary>
    public static bool IsValidAppointmentTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return TryParseTime(raw.Trim(), out _);
    }

    /// <summary>Normalizes to <c>HH:mm</c> for persistence and admin display.</summary>
    public static string NormalizeAppointmentTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        return TryParseTime(raw.Trim(), out var ts)
            ? ts.ToString(@"hh\:mm", CultureInfo.InvariantCulture)
            : raw.Trim();
    }

    private static bool TryParseTime(string t, out TimeSpan ts)
    {
        string[] formats = [@"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss"];
        if (TimeSpan.TryParseExact(t, formats, CultureInfo.InvariantCulture, out ts))
            return ts >= TimeSpan.Zero && ts < TimeSpan.FromDays(1);

        if (TimeSpan.TryParse(t, CultureInfo.InvariantCulture, out ts))
            return ts >= TimeSpan.Zero && ts < TimeSpan.FromDays(1);

        ts = default;
        return false;
    }

    public TestRideValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Vui lòng nhập họ tên.")
            .MaximumLength(100).WithMessage("Họ tên tối đa 100 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Vui lòng nhập số điện thoại.")
            .Matches(@"^(0|\+84)[0-9]{8,10}$")
            .WithMessage("Số điện thoại không hợp lệ.");

        RuleFor(x => x.MotorcycleId)
            .NotNull().WithMessage("Vui lòng chọn mẫu xe.")
            .Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithMessage("Vui lòng chọn mẫu xe.");

        RuleFor(x => x.AppointmentDate)
            .Must(d => d.Date >= TestRideVietnamTime.Today)
            .WithMessage("Ngày hẹn phải từ hôm nay trở đi.");

        RuleFor(x => x.AppointmentTime)
            .NotEmpty().WithMessage("Vui lòng chọn giờ hẹn.")
            .Must(IsValidAppointmentTime)
            .WithMessage("Giờ hẹn không hợp lệ.");

        RuleFor(x => x.Source).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.");
    }
}
