namespace HieuNga.Application.Interfaces;

public interface IImageStorageService
{
    /// <summary>Whether file upload is available in the current environment.</summary>
    bool SupportsUpload { get; }

    /// <summary>Human-readable note for Admin UI (e.g. local vs cloud persistence).</summary>
    string StorageDescription { get; }

    Task<ImageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default);
}

public sealed record ImageUploadResult(bool Success, string? PublicUrl, string? ErrorMessage)
{
    public static ImageUploadResult Ok(string publicUrl) => new(true, publicUrl, null);
    public static ImageUploadResult Fail(string error) => new(false, null, error);
}
