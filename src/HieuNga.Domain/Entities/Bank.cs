using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class Bank : BaseEntity
{
    public Guid BankTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? BrandColor { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public BankType BankType { get; set; } = null!;
    public ICollection<FinanceRate> FinanceRates { get; set; } = [];
}
