using HieuNga.Domain.Enums;

namespace HieuNga.Application.Media;

/// <summary>In-memory upload payload — Web layer adapts IFormFile to this.</summary>
public sealed class MediaFileUpload
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Length { get; init; }
    /// <summary>Optional relative path for smart folder import (e.g. colors/do.jpg).</summary>
    public string? RelativePath { get; init; }
}

/// <summary>
/// Domain service for motorcycle media. Owns the database state (thumbnail,
/// color cards with images, six fixed viewing-angle spin frames) and the
/// reorder/delete rules. Does NOT own the upload pipeline — callers must
/// resolve an image URL via <see cref="IImageUploadService"/> first, then call
/// the matching <c>*UrlAsync</c> method here.
/// </summary>
public interface IMotorcycleMediaStudioService
{
    Task<MediaStudioStateDto?> GetStateAsync(Guid motorcycleId, CancellationToken ct = default);

    Task<MediaMutationResult> SetSlotUrlAsync(Guid motorcycleId, MediaSlot slot, string url, CancellationToken ct = default);
    Task<MediaMutationResult> ClearSlotAsync(Guid motorcycleId, MediaSlot slot, CancellationToken ct = default);

    Task<MediaMutationResult> UpsertColorUrlAsync(Guid motorcycleId, Guid? colorId, string name, string hex, string? imageUrl, CancellationToken ct = default);
    Task<MediaMutationResult> ReplaceColorImageUrlAsync(Guid motorcycleId, Guid colorId, string imageUrl, CancellationToken ct = default);
    Task<MediaMutationResult> ReorderColorsAsync(Guid motorcycleId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);
    Task<MediaMutationResult> DeleteColorAsync(Guid motorcycleId, Guid colorId, CancellationToken ct = default);

    Task<MediaMutationResult> SetAngleUrlAsync(Guid motorcycleId, MotorcycleViewAngle angle, string url, CancellationToken ct = default);
    Task<MediaMutationResult> ClearAngleAsync(Guid motorcycleId, MotorcycleViewAngle angle, CancellationToken ct = default);
    Task<MediaMutationResult> ClearAllAnglesAsync(Guid motorcycleId, CancellationToken ct = default);

    /// <summary>
    /// Apply an uploaded URL to the slot inferred from <paramref name="relativePath"/>.
    /// Used by the smart-import endpoint to batch-assign uploads without leaking
    /// filename heuristics into the JS layer.
    /// </summary>
    Task<MediaMutationResult> AssignByPathAsync(Guid motorcycleId, string relativePath, string url, CancellationToken ct = default);
}
