namespace HieuNga.Application.Finance;

/// <summary>Shared installment math for detail calculator and listing teasers (flat estimate).</summary>
public static class FinanceMath
{
    public const decimal DefaultDownPaymentPercent = 20m;
    public const int DefaultTermMonths = 12;
    /// <summary>Matches seeded CMS partner rate (0,79%/tháng) when a bank rate is unavailable for teasers.</summary>
    public const decimal FallbackMonthlyRate = 0.0079m;

    public readonly record struct PaymentBreakdown(
        decimal Principal,
        decimal Monthly,
        decimal Total,
        decimal Interest);

    /// <summary>
    /// Flat estimate used by the public detail calculator:
    /// monthly = principal / term + principal × monthlyRate;
    /// interest = monthly × term − principal;
    /// total = down + monthly × term.
    /// </summary>
    public static PaymentBreakdown Compute(
        decimal price,
        decimal downPaymentPercent,
        int termMonths,
        decimal monthlyRate)
    {
        if (price <= 0m || termMonths <= 0)
            return default;

        var down = price * (downPaymentPercent / 100m);
        var principal = price - down;
        if (principal <= 0m)
            return new PaymentBreakdown(0m, 0m, down, 0m);

        var monthly = Math.Round(principal / termMonths + principal * monthlyRate, 0);
        var installmentTotal = monthly * termMonths;
        var interest = Math.Max(0m, installmentTotal - principal);
        return new PaymentBreakdown(principal, monthly, down + installmentTotal, interest);
    }

    public static decimal MonthlyPayment(
        decimal price,
        decimal downPaymentPercent,
        int termMonths,
        decimal monthlyRate) =>
        Compute(price, downPaymentPercent, termMonths, monthlyRate).Monthly;

    public static decimal EstimatedMonthly(decimal price, decimal monthlyRate = FallbackMonthlyRate) =>
        MonthlyPayment(price, DefaultDownPaymentPercent, DefaultTermMonths, monthlyRate);
}
