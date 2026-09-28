using HieuNga.Application.Interfaces;
using HieuNga.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HieuNga.Application.Media;

/// <summary>
/// Default <see cref="IImageUploadService"/>. Single canonical pipeline for every
/// admin image upload in the application.
///
/// Validation is centralized here so the storage providers and the domain endpoints
/// no longer duplicate MIME / extension / size rules.
/// </summary>
public sealed class ImageUploadService(
    IImageStorageService storage,
    IOptions<ImageStorageOptions> options,
    ILogger<ImageUploadService> logger) : IImageUploadService
{
    /// <summary>
    /// Allowed client-side MIME types. SVG is deliberately excluded: SVG can carry
    /// scripts and the local storage backend serves files as-is. Cloudinary already
    /// rejects SVG; excluding it here makes the rule consistent across providers.
    /// </summary>
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    /// <summary>
    /// All kinds supported by the canonical upload endpoint.
    /// </summary>
    public static readonly IReadOnlySet<string> Kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ImageUploadKinds.MotorcycleThumbnail,
        ImageUploadKinds.MotorcycleColor,
        ImageUploadKinds.MotorcycleAngle,
        ImageUploadKinds.MotorcycleFeature,
        ImageUploadKinds.MotorcycleTechnology,
        ImageUploadKinds.BlogThumbnail,
        ImageUploadKinds.Promotion,
        ImageUploadKinds.Banner,
        ImageUploadKinds.ServiceGallery,
        ImageUploadKinds.BankLogo
    };

    public IReadOnlySet<string> SupportedKinds => Kinds;

    public async Task<ImageUploadResponse> UploadAsync(
        ImageUploadPayload file,
        string kind,
        Guid? contextId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(kind) || !Kinds.Contains(kind))
            return ImageUploadResponse.Failure(
                ImageUploadErrorCodes.InvalidFile,
                $"Loại ảnh không hợp lệ: '{kind}'. Cho phép: {string.Join(", ", Kinds)}.");

        var validation = ValidateFile(file);
        if (validation is not null) return validation;

        if (!storage.SupportsUpload)
        {
            logger.LogWarning("Image upload attempted while storage backend is unavailable. Kind={Kind}", kind);
            return ImageUploadResponse.Failure(
                ImageUploadErrorCodes.StorageUnavailable,
                "Dịch vụ lưu ảnh chưa được cấu hình trên môi trường này. " +
                "Liên hệ quản trị viên hoặc dùng URL ảnh thay thế.");
        }

        var folder = ResolveFolder(kind, contextId);
        try
        {
            if (file.Content.CanSeek) file.Content.Position = 0;
            var result = await storage.UploadAsync(
                file.Content, file.FileName ?? "upload", file.ContentType ?? "application/octet-stream",
                folder, ct);

            if (!result.Success || string.IsNullOrWhiteSpace(result.EffectiveUrl))
            {
                logger.LogWarning("Storage upload failed for kind={Kind}: {Error}", kind, result.ErrorMessage);
                return ImageUploadResponse.Failure(
                    ImageUploadErrorCodes.StorageUploadFailed,
                    result.ErrorMessage ?? "Không tải được ảnh lên dịch vụ lưu trữ.");
            }

            return ImageUploadResponse.Success(
                result.EffectiveUrl!,
                result.Width,
                result.Height,
                result.Bytes ?? file.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during image upload. Kind={Kind}", kind);
            return ImageUploadResponse.Failure(
                ImageUploadErrorCodes.UnknownError,
                "Lỗi không mong đợi khi tải ảnh. Vui lòng thử lại.");
        }
    }

    private ImageUploadResponse? ValidateFile(ImageUploadPayload? file)
    {
        if (file is null || file.Length <= 0)
            return ImageUploadResponse.Failure(
                ImageUploadErrorCodes.InvalidFile,
                "Chưa chọn ảnh hoặc ảnh rỗng.");

        if (string.IsNullOrWhiteSpace(file.FileName))
            return ImageUploadResponse.Failure(
                ImageUploadErrorCodes.InvalidFile,
                "Tên file không hợp lệ.");

        var ext = System.IO.Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return ImageUploadResponse.Failure(
                ImageUploadErrorCodes.UnsupportedType,
                "Chỉ chấp nhận ảnh JPG, PNG hoặc WebP.");

        var declaredType = file.ContentType?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(declaredType) || !AllowedContentTypes.Contains(declaredType))
            return ImageUploadResponse.Failure(
                ImageUploadErrorCodes.UnsupportedType,
                "Loại nội dung không được phép. Chỉ chấp nhận JPG, PNG hoặc WebP.");

        var maxBytes = (long)options.Value.MaxFileSizeMb * 1024L * 1024L;
        if (file.Length > maxBytes)
            return ImageUploadResponse.Failure(
                ImageUploadErrorCodes.FileTooLarge,
                $"Ảnh vượt quá kích thước tối đa {options.Value.MaxFileSizeMb} MB.");

        return null;
    }

    /// <summary>
    /// Map a logical kind (+ optional entity context) to a safe storage folder.
    /// The client never controls this — the folder is always derived server-side.
    /// When <paramref name="contextId"/> is null/empty the kind falls back to a
    /// static folder name (used for upload-before-create flows such as the
    /// motorcycle "quick create" handler).
    /// </summary>
    private static string ResolveFolder(string kind, Guid? contextId)
    {
        var id = contextId.HasValue && contextId.Value != Guid.Empty ? contextId.Value.ToString("N") : null;
        return kind.ToLowerInvariant() switch
        {
            ImageUploadKinds.MotorcycleThumbnail => id is null ? "motorcycles" : $"motorcycles/{id}/thumb",
            ImageUploadKinds.MotorcycleColor     => id is null ? "motorcycles/colors" : $"motorcycles/{id}/colors",
            ImageUploadKinds.MotorcycleAngle     => id is null ? "motorcycles/angles" : $"motorcycles/{id}/angles",
            ImageUploadKinds.MotorcycleFeature   => id is null ? "motorcycle-features" : $"motorcycles/{id}/features",
            ImageUploadKinds.MotorcycleTechnology => id is null ? "motorcycle-technologies" : $"motorcycles/{id}/technologies",
            ImageUploadKinds.ServiceGallery      => id is null ? "services" : $"services/{id}",
            ImageUploadKinds.BlogThumbnail       => "blog",
            ImageUploadKinds.Promotion           => "promotions",
            ImageUploadKinds.Banner              => "banners",
            ImageUploadKinds.BankLogo            => "banks",
            _ => "general"
        };
    }
}
