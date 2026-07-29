using FluentValidation;
using HieuNga.Application.DTOs;

namespace HieuNga.Application.Validators;

public class CreateMaintenanceBookingValidator : AbstractValidator<CreateMaintenanceBookingDto>
{
    public static readonly string[] AllowedTimes =
    [
        "08:00", "08:30", "09:00", "09:30", "10:00", "10:30",
        "11:00", "11:30", "13:30", "14:00", "14:30", "15:00",
        "15:30", "16:00", "16:30", "17:00"
    ];

    public CreateMaintenanceBookingValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100)
            .WithMessage("Vui lòng nhập họ tên.");
        RuleFor(x => x.Phone).NotEmpty()
            .Matches(@"^(0|\+84)[0-9]{8,10}$")
            .WithMessage("Số điện thoại không hợp lệ.");
        RuleFor(x => x.MotorcycleModel).NotEmpty().MaximumLength(100)
            .WithMessage("Vui lòng nhập dòng xe.");
        RuleFor(x => x.ServiceType).NotEmpty().MaximumLength(200)
            .WithMessage("Vui lòng chọn dịch vụ.");
        RuleFor(x => x.PreferredDate)
            .Must(d => d.Date >= DateTime.Today)
            .WithMessage("Ngày hẹn phải từ hôm nay trở đi.");
        RuleFor(x => x.PreferredTime).NotEmpty()
            .Must(t => AllowedTimes.Contains(t))
            .WithMessage("Vui lòng chọn giờ hẹn.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
