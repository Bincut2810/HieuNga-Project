using FluentValidation;
using HieuNga.Application.DTOs;

namespace HieuNga.Application.Validators;

public class CreateBookingValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^(0|\+84)[0-9]{8,10}$").WithMessage("Số điện thoại không hợp lệ");
        RuleFor(x => x.PreferredDate).GreaterThan(DateTime.Today);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}
