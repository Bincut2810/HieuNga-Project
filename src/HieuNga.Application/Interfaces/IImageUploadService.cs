namespace HieuNga.Application.Media;

/// <summary>
/// Plain payload passed from the Web layer to the Application upload pipeline.
/// Decoupled from ASP.NET Core so the Application layer doesn't depend on
/// <c>Microsoft.AspNetCore.Http</c>.
/// </summary>
public sealed class ImageUploadPayload
{
    /// <summary>Open, readable stream. Ownership transfers to the service — caller should not dispose.</summary>
    public required Stream Content { get; init; }

    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
}

/// <summary>
/// Canonical, single-purpose image-upload application service.
///
/// Responsibilities:
///   - Validate file (size, content type, extension) using the configured rules.
///   - Map a logical <c>kind</c> (e.g. "promotion", "motorcycle-thumbnail") to a safe
///     storage folder. The client never controls the storage folder directly.
///   - Delegate physical persistence to <see cref="Interfaces.IImageStorageService"/>.
///   - Return a normalized <see cref="ImageUploadResponse"/> (and never throw for
///     expected user errors).
///
/// Domain services (motorcycle media, banner CMS, service CMS) compose with this
/// service rather than re-implementing validation/storage logic.
/// </summary>
public interface IImageUploadService
{
    /// <summary>All <c>kind</c> values the service accepts.</summary>
    IReadOnlySet<string> SupportedKinds { get; }

    /// <summary>
    /// Validates and stores the uploaded file. <paramref name="contextId"/> is optional —
    /// it is included in the storage folder when present (e.g. motorcycle colors and
    /// angles need the motorcycle id, service gallery needs the service id).
    /// </summary>
    Task<ImageUploadResponse> UploadAsync(
        ImageUploadPayload file,
        string kind,
        Guid? contextId = null,
        CancellationToken ct = default);
}
