namespace HieuNga.Application.Media;

public enum MediaSlot
{
    Thumbnail,
    Hero,
    Gallery,
    Color,
    Angles
}

public sealed record MediaStudioStateDto(
    Guid MotorcycleId,
    string Name,
    string Slug,
    bool SupportsUpload,
    string StorageNote,
    MediaSlotDto? Thumbnail,
    MediaSlotDto? Hero,
    IReadOnlyList<GalleryItemDto> Gallery,
    IReadOnlyList<ColorCardDto> Colors,
    AngleStudioDto Angles,
    MediaHealthDto Health,
    PublishReadinessDto Publish);

public sealed record MediaSlotDto(
    string Url,
    int? Width,
    int? Height,
    long? Bytes,
    string? FileName);

public sealed record GalleryItemDto(
    Guid Id,
    string Url,
    string FileName,
    string? AltText,
    int SortOrder,
    int? Width,
    int? Height,
    long? Bytes);

public sealed record ColorCardDto(
    Guid Id,
    string Name,
    string HexCode,
    string? ImageUrl,
    int SortOrder,
    int GalleryCount,
    int AngleFilledCount);

public sealed record AngleSlotDto(
    string Key,
    string Label,
    int Angle,
    Guid? Id,
    string? Url);

public sealed record AngleStudioDto(
    IReadOnlyList<AngleSlotDto> Slots,
    int FilledCount,
    int Total,
    bool IsComplete,
    string StatusLabel);

public sealed record MediaHealthItemDto(string Key, string Label, string Status, string Detail);

public sealed record MediaHealthDto(
    int ScorePercent,
    IReadOnlyList<MediaHealthItemDto> Items);

public sealed record PublishReadinessDto(
    bool Ready,
    string StatusLabel,
    IReadOnlyList<string> Missing);

public sealed record MediaMutationResult(bool Success, string? Message, MediaStudioStateDto? State);

public sealed record SmartImportSummaryDto(
    bool Success,
    string? Message,
    int Thumbnail,
    int Hero,
    int Gallery,
    int Colors,
    int Angles,
    IReadOnlyList<string> Warnings,
    MediaStudioStateDto? State);
