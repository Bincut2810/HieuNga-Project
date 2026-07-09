using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class FinanceRate : BaseEntity
{
    public Guid BankId { get; set; }
    public string PlanName { get; set; } = "Trả góp tiêu chuẩn";
    public decimal MonthlyInterestRatePercent { get; set; }
    public int MinDownPaymentPercent { get; set; } = 0;
    public int MaxDownPaymentPercent { get; set; } = 70;
    public int MinTermMonths { get; set; } = 6;
    public int MaxTermMonths { get; set; } = 36;
    /// <summary>Comma-separated supported terms, e.g. "6,12,18,24,36".</summary>
    public string? SupportedTermsMonths { get; set; }
    public string? ProcessingFeeText { get; set; }
    public string? Note { get; set; }
    public string? TrustLabel { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public Bank Bank { get; set; } = null!;
}
