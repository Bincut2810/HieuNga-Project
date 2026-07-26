namespace HieuNga.Application.Media;

public enum MediaSlot
{
    Thumbnail,
    Hero,
    Gallery,
    Color,
    Spin
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
    SpinStudioDto Spin,
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
    int SpinCount);

public sealed record SpinFrameDto(Guid Id, string Url, int FrameIndex, string Label);

public sealed record SpinStudioDto(
    IReadOnlyList<SpinFrameDto> Frames,
    int ExpectedFrames,
    int PresentCount,
    IReadOnlyList<int> MissingIndices,
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
    int SpinFrames,
    IReadOnlyList<string> Warnings,
    MediaStudioStateDto? State);
