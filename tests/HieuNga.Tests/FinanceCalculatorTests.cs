using HieuNga.Application.DTOs;
using HieuNga.Application.Finance;

namespace HieuNga.Tests;

public class MotorcyclePricingTests
{
    [Fact]
    public void EffectivePrice_uses_first_positive_variant()
    {
        var price = MotorcyclePricing.ResolveEffectivePrice(50_000_000m, [0m, 42_000_000m, 45_000_000m]);
        Assert.Equal(42_000_000m, price);
    }

    [Fact]
    public void EffectivePrice_falls_back_to_base_when_no_positive_variant()
    {
        var price = MotorcyclePricing.ResolveEffectivePrice(39_000_000m, [0m, 0m]);
        Assert.Equal(39_000_000m, price);
    }

    [Fact]
    public void EffectivePrice_zero_when_base_and_variants_are_zero()
    {
        Assert.Equal(0m, MotorcyclePricing.ResolveEffectivePrice(0m, [0m]));
        Assert.Equal(0m, MotorcyclePricing.ResolveEffectivePrice(0m, Array.Empty<decimal>()));
    }

    [Fact]
    public void EffectivePrice_variant_dto_overload_matches()
    {
        var variants = new List<MotorcycleVariantDto>
        {
            new(Guid.NewGuid(), "A", 0m, 1, true),
            new(Guid.NewGuid(), "B", 41_500_000m, 2, true)
        };
        Assert.Equal(41_500_000m, MotorcyclePricing.ResolveEffectivePrice(40_000_000m, variants));
    }
}

public class FinanceMathTests
{
    [Fact]
    public void MonthlyPayment_flat_formula()
    {
        // price 50M, 20% down → principal 40M; 12 months @ 0.79%/mo
        // monthly = 40M/12 + 40M*0.0079 = 3,333,333.33 + 316,000 ≈ 3,649,333
        var monthly = FinanceMath.MonthlyPayment(50_000_000m, 20m, 12, 0.0079m);
        Assert.Equal(3_649_333m, monthly);
    }

    [Fact]
    public void MonthlyPayment_zero_price_returns_zero()
    {
        Assert.Equal(0m, FinanceMath.MonthlyPayment(0m, 20m, 12, 0.0079m));
    }

    [Fact]
    public void EstimatedMonthly_uses_defaults()
    {
        var expected = FinanceMath.MonthlyPayment(
            50_000_000m,
            FinanceMath.DefaultDownPaymentPercent,
            FinanceMath.DefaultTermMonths,
            FinanceMath.FallbackMonthlyRate);
        Assert.Equal(expected, FinanceMath.EstimatedMonthly(50_000_000m));
    }

    [Fact]
    public void Compute_matches_monthly_interest_and_total()
    {
        var b = FinanceMath.Compute(50_000_000m, 20m, 12, 0.0079m);
        Assert.Equal(40_000_000m, b.Principal);
        Assert.Equal(3_649_333m, b.Monthly);
        Assert.Equal(b.Monthly * 12, b.Total - (50_000_000m * 0.2m));
        Assert.Equal(b.Monthly * 12 - b.Principal, b.Interest);
    }
}

public class FinanceCalculatorViewModelTests
{
    private static FinanceBankDto Bank(string id, bool isDefault = false, decimal rate = 0.0079m) =>
        new(id, "Bank " + id, "B", rate, rate * 100m, $"{rate * 100m:0.##}%/tháng",
            null, "#000", 10, 70, [6, 12, 24], isDefault);

    [Fact]
    public void Create_enabled_when_price_and_banks_exist()
    {
        var vm = FinanceCalculatorViewModel.Create(40_000_000m, [Bank("a", isDefault: true)]);
        Assert.True(vm.CalculatorEnabled);
        Assert.Equal(40_000_000m, vm.Price);
        Assert.Equal("a", vm.DefaultBankId);
        Assert.Equal(FinanceMath.DefaultDownPaymentPercent, vm.DefaultDownPaymentPercent);
        Assert.Equal(FinanceMath.DefaultTermMonths, vm.DefaultTermMonths);
        Assert.Single(vm.Banks);
        Assert.True(vm.EstimatedMonthlyPayment > 0);
    }

    [Fact]
    public void Create_disabled_when_price_is_zero()
    {
        var vm = FinanceCalculatorViewModel.Create(0m, [Bank("a")]);
        Assert.False(vm.CalculatorEnabled);
        Assert.Equal(0m, vm.EstimatedMonthlyPayment);
    }

    [Fact]
    public void Create_disabled_when_no_banks()
    {
        var vm = FinanceCalculatorViewModel.Create(40_000_000m, []);
        Assert.False(vm.CalculatorEnabled);
        Assert.Null(vm.DefaultBankId);
        Assert.Equal(0m, vm.EstimatedMonthlyPayment);
    }

    [Fact]
    public void Create_picks_first_bank_when_no_default_flag()
    {
        var vm = FinanceCalculatorViewModel.Create(40_000_000m, [Bank("x"), Bank("y", isDefault: false)]);
        Assert.Equal("x", vm.DefaultBankId);
    }
}
