using HieuNga.Application.DTOs;

namespace HieuNga.Application.Interfaces;

public interface IInstallmentService
{
    InstallmentCalculationDto Calculate(
        decimal vehiclePrice,
        decimal downPayment,
        int termMonths,
        decimal? monthlyRate = null,
        decimal annualRate = 0.12m,
        string? bankName = null);
    Task<Guid> SubmitRequestAsync(CreateInstallmentRequestDto dto, CancellationToken ct = default);
}
