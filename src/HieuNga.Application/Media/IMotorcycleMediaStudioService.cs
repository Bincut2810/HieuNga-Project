using HieuNga.Domain.Enums;

namespace HieuNga.Application.Media;

/// <summary>In-memory upload payload — Web layer adapts IFormFile to this.</summary>
public sealed class MediaFileUpload
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Length { get; init; }
    /// <summary>Optional relative path for smart folder import (e.g. gallery/01.jpg).</summary>
    public string? RelativePath { get; init; }
}

public interface IMotorcycleMediaStudioService
{
    Task<MediaStudioStateDto?> GetStateAsync(Guid motorcycleId, CancellationToken ct = default);

    Task<MediaMutationResult> SetSlotAsync(Guid motorcycleId, MediaSlot slot, MediaFileUpload file, CancellationToken ct = default);
    Task<MediaMutationResult> ClearSlotAsync(Guid motorcycleId, MediaSlot slot, CancellationToken ct = default);

    Task<MediaMutationResult> AddGalleryAsync(Guid motorcycleId, IReadOnlyList<MediaFileUpload> files, CancellationToken ct = default);
    Task<MediaMutationResult> ReplaceGalleryAsync(Guid motorcycleId, Guid mediaId, MediaFileUpload file, CancellationToken ct = default);
    Task<MediaMutationResult> UpdateGalleryCaptionAsync(Guid motorcycleId, Guid mediaId, string? caption, CancellationToken ct = default);
    Task<MediaMutationResult> ReorderGalleryAsync(Guid motorcycleId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);
    Task<MediaMutationResult> DeleteGalleryAsync(Guid motorcycleId, IReadOnlyList<Guid> mediaIds, CancellationToken ct = default);

    Task<MediaMutationResult> UpsertColorAsync(Guid motorcycleId, Guid? colorId, string name, string hex, MediaFileUpload? image, CancellationToken ct = default);
    Task<MediaMutationResult> ReplaceColorImageAsync(Guid motorcycleId, Guid colorId, MediaFileUpload file, CancellationToken ct = default);
    Task<MediaMutationResult> ReorderColorsAsync(Guid motorcycleId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);
    Task<MediaMutationResult> DeleteColorAsync(Guid motorcycleId, Guid colorId, CancellationToken ct = default);

    Task<MediaMutationResult> SetAngleAsync(Guid motorcycleId, MotorcycleViewAngle angle, MediaFileUpload file, CancellationToken ct = default);
    Task<MediaMutationResult> ClearAngleAsync(Guid motorcycleId, MotorcycleViewAngle angle, CancellationToken ct = default);
    Task<MediaMutationResult> ClearAllAnglesAsync(Guid motorcycleId, CancellationToken ct = default);

    Task<SmartImportSummaryDto> SmartImportAsync(Guid motorcycleId, IReadOnlyList<MediaFileUpload> entries, CancellationToken ct = default);

    Task<(bool Ok, string? Url, string? Error)> UploadOnlyAsync(MediaFileUpload file, string folder, CancellationToken ct = default);
}
