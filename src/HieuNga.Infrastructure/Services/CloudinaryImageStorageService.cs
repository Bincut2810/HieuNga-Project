using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppImageUploadResult = HieuNga.Application.Interfaces.ImageUploadResult;

namespace HieuNga.Infrastructure.Services;

public sealed class CloudinaryImageStorageService(
    IOptions<ImageStorageOptions> options,
    ILogger<CloudinaryImageStorageService> logger) : IImageStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
    };

    public bool SupportsUpload => options.Value.Cloudinary.IsConfigured;

    public string StorageDescription =>
        SupportsUpload
            ? "Lưu trên Cloudinary — ảnh tồn tại sau khi redeploy container."
            : "Cloudinary chưa cấu hình. Dùng URL ảnh hoặc cấu hình ImageStorage__Cloudinary__* trên hosting.";

    public async Task<AppImageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsUpload)
            return AppImageUploadResult.Fail("Cloudinary chưa được cấu hình trên server.");

        if (!AllowedContentTypes.Contains(contentType))
            return AppImageUploadResult.Fail("Chỉ chấp nhận ảnh JPG, PNG, WebP hoặc GIF.");

        var maxBytes = options.Value.MaxFileSizeMb * 1024L * 1024L;
        if (content.CanSeek && content.Length > maxBytes)
            return AppImageUploadResult.Fail($"Ảnh vượt quá {options.Value.MaxFileSizeMb} MB.");

        var cloudinary = BuildClient();
        var publicId = $"{SanitizeFolder(folder)}/{Guid.NewGuid():N}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = publicId,
            Overwrite = false,
            Folder = "hieunga"
        };

        var result = await cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (result.Error is not null)
        {
            logger.LogWarning("Cloudinary upload failed: {Message}", result.Error.Message);
            return AppImageUploadResult.Fail("Không thể tải ảnh lên Cloudinary. Vui lòng thử lại hoặc dùng URL.");
        }

        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url))
            return AppImageUploadResult.Fail("Cloudinary không trả về URL ảnh.");

        var delivery = ToAutoOptimizedUrl(url);
        logger.LogInformation("Uploaded image to Cloudinary folder {Folder}", folder);
        return AppImageUploadResult.Ok(
            url,
            result.Width > 0 ? result.Width : null,
            result.Height > 0 ? result.Height : null,
            result.Bytes > 0 ? result.Bytes : null,
            delivery);
    }

    /// <summary>Inserts f_auto,q_auto into Cloudinary delivery URL for WebP/AVIF when supported.</summary>
    private static string ToAutoOptimizedUrl(string secureUrl)
    {
        const string marker = "/upload/";
        var idx = secureUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return secureUrl;
        var insertAt = idx + marker.Length;
        if (secureUrl.IndexOf("f_auto", insertAt, StringComparison.OrdinalIgnoreCase) >= 0)
            return secureUrl;
        return secureUrl.Insert(insertAt, "f_auto,q_auto/");
    }

    private Cloudinary BuildClient()
    {
        var cfg = options.Value.Cloudinary;
        var account = new Account(cfg.CloudName, cfg.ApiKey, cfg.ApiSecret);
        return new Cloudinary(account);
    }

    private static string SanitizeFolder(string folder)
    {
        var cleaned = new string(folder
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            .ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "general" : cleaned;
    }
}
