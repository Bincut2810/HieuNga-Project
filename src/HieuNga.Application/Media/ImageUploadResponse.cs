namespace HieuNga.Application.Media;

/// <summary>
/// Canonical, normalized response shape for every image upload operation in the application.
/// All upload endpoints (canonical /admin/api/upload and the domain endpoints that delegate
/// to <see cref="IImageUploadService"/>) return this shape so the frontend can rely on a
/// single contract.
///
/// Wire format is JSON with snake/camel-compatible lowerCamelCase serialization handled
/// by ASP.NET Core's default System.Text.Json configuration.
/// </summary>
public sealed record ImageUploadResponse(
    bool Ok,
    string? Url = null,
    string? Code = null,
    string? Message = null,
    int? Width = null,
    int? Height = null,
    long? Bytes = null)
{
    public static ImageUploadResponse Success(string url, int? width = null, int? height = null, long? bytes = null) =>
        new(true, url, null, null, width, height, bytes);

    public static ImageUploadResponse Failure(string code, string message) =>
        new(false, null, code, message);
}

/// <summary>
/// Stable machine-readable error codes for image-upload failures. Frontend widgets branch
/// on these (instead of parsing human messages) so messages can be translated freely.
/// </summary>
public static class ImageUploadErrorCodes
{
    /// <summary>The file is missing, empty, or has no usable name.</summary>
    public const string InvalidFile = "INVALID_FILE";

    /// <summary>The file exceeds the configured maximum size.</summary>
    public const string FileTooLarge = "FILE_TOO_LARGE";

    /// <summary>The content type / extension is not in the allowed image set.</summary>
    public const string UnsupportedType = "UNSUPPORTED_TYPE";

    /// <summary>The caller is not authenticated.</summary>
    public const string Unauthorized = "UNAUTHORIZED";

    /// <summary>No storage backend is configured (e.g. Cloudinary selected but no credentials).</summary>
    public const string StorageUnavailable = "STORAGE_UNAVAILABLE";

    /// <summary>The configured storage backend rejected the upload.</summary>
    public const string StorageUploadFailed = "STORAGE_UPLOAD_FAILED";

    /// <summary>Generic catch-all for unexpected server failures.</summary>
    public const string UnknownError = "UNKNOWN_ERROR";
}

/// <summary>Known storage "kinds" the canonical upload endpoint accepts. Maps to a safe folder.</summary>
public static class ImageUploadKinds
{
    public const string MotorcycleThumbnail = "motorcycle-thumbnail";
    public const string MotorcycleColor = "motorcycle-color";
    public const string MotorcycleAngle = "motorcycle-angle";
    public const string MotorcycleFeature = "motorcycle-feature";
    public const string MotorcycleTechnology = "motorcycle-technology";
    public const string BlogThumbnail = "blog-thumbnail";
    public const string Promotion = "promotion";
    public const string Banner = "banner";
    public const string ServiceGallery = "service-gallery";
    public const string BankLogo = "bank-logo";
}
