using HieuNga.Application.Interfaces;
using HieuNga.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HieuNga.Infrastructure.Services;

public sealed class LocalImageStorageService(
    IHostEnvironment environment,
    IOptions<ImageStorageOptions> options,
    ILogger<LocalImageStorageService> logger) : IImageStorageService
{
    /// <summary>
    /// Allowed MIME types. SVG is deliberately excluded: SVG can carry scripts
    /// and the local backend serves files as-is. The canonical
    /// <see cref="ImageUploadService"/> enforces this rule; the local provider
    /// repeats it as a defense-in-depth check for any non-canonical entry point.
    /// </summary>
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp"
    };

    public bool SupportsUpload => true;

    public string StorageDescription =>
        "Lưu trên đĩa cục bộ (wwwroot/uploads). Phù hợp Development — file có thể mất khi container restart trên hosting miễn phí.";

    public async Task<ImageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(content, fileName, contentType);
        if (validation is not null)
            return ImageUploadResult.Fail(validation);

        var safeFolder = SanitizeFolder(folder);
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        // SVG is forbidden everywhere in the new pipeline.
        if (string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
            return ImageUploadResult.Fail("Không chấp nhận ảnh SVG.");

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var uploadsRoot = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", safeFolder);
        Directory.CreateDirectory(uploadsRoot);

        var physicalPath = Path.Combine(uploadsRoot, storedName);
        await using (var fileStream = File.Create(physicalPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var publicUrl = $"/uploads/{safeFolder}/{storedName}";
        logger.LogInformation("Stored image at {PublicUrl}", publicUrl);
        long? bytes = null;
        try { bytes = new FileInfo(physicalPath).Length; } catch { /* ignore */ }
        return ImageUploadResult.Ok(publicUrl, bytes: bytes);
    }

    private string? Validate(Stream content, string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "Tên file không hợp lệ.";

        if (!AllowedContentTypes.Contains(contentType))
            return "Chỉ chấp nhận ảnh JPG, PNG hoặc WebP.";

        var maxBytes = options.Value.MaxFileSizeMb * 1024L * 1024L;
        if (content.CanSeek && content.Length > maxBytes)
            return $"Ảnh vượt quá {options.Value.MaxFileSizeMb} MB.";

        return null;
    }

    private static string SanitizeFolder(string folder)
    {
        var cleaned = new string(folder
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '/')
            .ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "general" : cleaned.Trim('/');
    }
}
