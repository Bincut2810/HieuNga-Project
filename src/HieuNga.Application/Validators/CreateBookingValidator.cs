using FluentValidation;
using HieuNga.Application.DTOs;

namespace HieuNga.Application.Validators;

public class CreateBookingValidator : AbstractValidator<CreateBookingDto>
{
    public static readonly string[] AllowedTimeSlots =
    [
        "08:00–10:00",
        "10:00–12:00",
        "13:00–15:00",
        "15:00–17:00"
    ];

    public CreateBookingValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100)
            .WithMessage("Vui lòng nhập họ tên.");
        RuleFor(x => x.Phone).NotEmpty()
            .Matches(@"^(0|\+84)[0-9]{8,10}$")
            .WithMessage("Số điện thoại không hợp lệ.");
        RuleFor(x => x.PreferredDate)
            .Must(d => d.Date >= DateTime.Today)
            .WithMessage("Ngày hẹn phải từ hôm nay trở đi.");
        RuleFor(x => x.PreferredTime).NotEmpty()
            .Must(t => AllowedTimeSlots.Contains(t))
            .WithMessage("Vui lòng chọn khung giờ.");
        RuleFor(x => x.Email).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email không hợp lệ.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
