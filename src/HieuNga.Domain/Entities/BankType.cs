using HieuNga.Domain.Common;

namespace HieuNga.Domain.Entities;

public class BankType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Bank> Banks { get; set; } = [];
}
