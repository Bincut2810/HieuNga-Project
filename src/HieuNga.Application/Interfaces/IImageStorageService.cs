namespace HieuNga.Application.Interfaces;

public interface IImageStorageService
{
    bool SupportsUpload { get; }
    string StorageDescription { get; }

    Task<ImageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of a storage upload. DeliveryUrl may be an optimized CDN URL (e.g. Cloudinary f_auto).</summary>
public sealed record ImageUploadResult(
    bool Success,
    string? PublicUrl,
    string? ErrorMessage,
    int? Width = null,
    int? Height = null,
    long? Bytes = null,
    string? DeliveryUrl = null)
{
    public string? EffectiveUrl =>
        !string.IsNullOrWhiteSpace(DeliveryUrl) ? DeliveryUrl : PublicUrl;

    public static ImageUploadResult Ok(
        string publicUrl,
        int? width = null,
        int? height = null,
        long? bytes = null,
        string? deliveryUrl = null) =>
        new(true, publicUrl, null, width, height, bytes, deliveryUrl);

    public static ImageUploadResult Fail(string error) =>
        new(false, null, error);
}
