using FluentValidation;

namespace HieuNga.Application.TestRide;

public sealed class TestRideValidator : AbstractValidator<TestRideRequest>
{
    public static readonly string[] AllowedAppointmentTimes =
    [
        "09:00",
        "10:00",
        "14:00",
        "16:00"
    ];

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
            .Must(t => AllowedAppointmentTimes.Contains(t))
            .WithMessage("Giờ hẹn không hợp lệ.");

        RuleFor(x => x.Source).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.");
    }
}
