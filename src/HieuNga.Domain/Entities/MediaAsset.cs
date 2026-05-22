using HieuNga.Domain.Common;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Entities;

public class MediaAsset : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public MediaType Type { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public Guid? MotorcycleId { get; set; }
    public int SortOrder { get; set; }

    public Motorcycle? Motorcycle { get; set; }
}
