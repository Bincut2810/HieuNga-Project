using HieuNga.Application.DTOs;

namespace HieuNga.Application.Finance;

/// <summary>Minimal ViewModel for the public detail finance calculator.</summary>
public sealed class FinanceCalculatorViewModel
{
    public decimal Price { get; init; }
    public string Currency { get; init; } = "VND";
    public IReadOnlyList<FinanceBankDto> Banks { get; init; } = [];
    public string? DefaultBankId { get; init; }
    public decimal DefaultDownPaymentPercent { get; init; } = FinanceMath.DefaultDownPaymentPercent;
    public int DefaultTermMonths { get; init; } = FinanceMath.DefaultTermMonths;
    public bool CalculatorEnabled { get; init; }

    public static FinanceCalculatorViewModel Create(decimal effectivePrice, IReadOnlyList<FinanceBankDto> banks)
    {
        banks ??= [];
        var defaultBank = banks.FirstOrDefault(b => b.IsDefault) ?? banks.FirstOrDefault();
        var enabled = effectivePrice > 0m && banks.Count > 0;

        return new FinanceCalculatorViewModel
        {
            Price = effectivePrice,
            Currency = "VND",
            Banks = banks,
            DefaultBankId = defaultBank?.Id,
            DefaultDownPaymentPercent = FinanceMath.DefaultDownPaymentPercent,
            DefaultTermMonths = FinanceMath.DefaultTermMonths,
            CalculatorEnabled = enabled
        };
    }

    public decimal EstimatedMonthlyPayment
    {
        get
        {
            if (!CalculatorEnabled || Price <= 0m) return 0m;
            var bank = Banks.FirstOrDefault(b => b.Id == DefaultBankId) ?? Banks.FirstOrDefault();
            var rate = bank?.MonthlyRate ?? FinanceMath.FallbackMonthlyRate;
            var breakdown = FinanceMath.Compute(Price, DefaultDownPaymentPercent, DefaultTermMonths, rate);
            return breakdown.Monthly;
        }
    }
}
