using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class InstallmentService(IRepository<InstallmentRequest> repository, IUnitOfWork unitOfWork) : IInstallmentService
{
    public InstallmentCalculationDto Calculate(
        decimal vehiclePrice,
        decimal downPayment,
        int termMonths,
        decimal? monthlyRate = null,
        decimal annualRate = 0.12m,
        string? bankName = null)
    {
        var principal = vehiclePrice - downPayment;
        if (principal <= 0 || termMonths <= 0)
            return new InstallmentCalculationDto(vehiclePrice, downPayment, termMonths, 0, downPayment, 0, 0, bankName, (monthlyRate ?? annualRate / 12m) * 100);

        var rate = monthlyRate ?? annualRate / 12m;
        var monthlyPayment = rate == 0
            ? principal / termMonths
            : principal * (rate * (decimal)Math.Pow((double)(1 + rate), termMonths))
              / ((decimal)Math.Pow((double)(1 + rate), termMonths) - 1);

        monthlyPayment = Math.Round(monthlyPayment, 0);
        var totalPayment = downPayment + monthlyPayment * termMonths;
        var totalInterest = Math.Max(0, totalPayment - vehiclePrice);

        return new InstallmentCalculationDto(
            vehiclePrice, downPayment, termMonths, monthlyPayment, totalPayment, totalInterest,
            principal, bankName, rate * 100);
    }

    public async Task<Guid> SubmitRequestAsync(CreateInstallmentRequestDto dto, CancellationToken ct = default)
    {
        var calc = Calculate(dto.VehiclePrice, dto.DownPayment, dto.TermMonths);
        var request = new InstallmentRequest
        {
            CustomerName = dto.CustomerName,
            Phone = dto.Phone,
            Email = dto.Email,
            MotorcycleId = dto.MotorcycleId,
            VehiclePrice = dto.VehiclePrice,
            DownPayment = dto.DownPayment,
            TermMonths = dto.TermMonths,
            MonthlyPayment = calc.MonthlyPayment,
            Notes = dto.Notes
        };

        await repository.AddAsync(request, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return request.Id;
    }
}
