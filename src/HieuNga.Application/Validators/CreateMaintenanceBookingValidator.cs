using FluentValidation;
using HieuNga.Application.DTOs;
using HieuNga.Application.TestRide;

namespace HieuNga.Application.Validators;

public class CreateMaintenanceBookingValidator : AbstractValidator<CreateMaintenanceBookingDto>
{
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
            .Must(d => d.Date >= TestRideVietnamTime.Today)
            .WithMessage("Ngày hẹn phải từ hôm nay trở đi.");
        RuleFor(x => x.PreferredTime).NotEmpty()
            .Must(TestRideValidator.IsValidAppointmentTime)
            .WithMessage("Vui lòng chọn giờ hẹn.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
