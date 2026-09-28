using HieuNga.Application.Interfaces;
using HieuNga.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HieuNga.Infrastructure.Services;

/// <summary>
/// Routes uploads to either <see cref="CloudinaryImageStorageService"/> or
/// <see cref="LocalImageStorageService"/> based on <see cref="ImageStorageOptions"/>.
///
/// <para>
/// Selection rules:
///   • If <c>ImageStorage:Provider = "Cloudinary"</c> AND Cloudinary credentials are
///     present → Cloudinary.
///   • Otherwise → Local (regardless of environment).
/// </para>
///
/// <para>
/// There is no longer a <c>DisabledImageStorageService</c>. When Cloudinary is
/// selected but credentials are missing, the router delegates to Cloudinary,
/// which then returns <c>SupportsUpload = false</c>. The canonical
/// <see cref="ImageUploadService"/> translates that into a stable
/// <c>STORAGE_UNAVAILABLE</c> response with HTTP 503, so the UI surfaces a
/// visible error instead of silently disabling the uploader.
/// </para>
/// </summary>
public sealed class ImageStorageRouter(
    IServiceProvider services,
    IOptions<ImageStorageOptions> options) : IImageStorageService
{
    private IImageStorageService Resolve()
    {
        var cfg = options.Value;
        if (cfg.UseCloudinary)
            return services.GetRequiredService<CloudinaryImageStorageService>();
        return services.GetRequiredService<LocalImageStorageService>();
    }

    public bool SupportsUpload => Resolve().SupportsUpload;
    public string StorageDescription => Resolve().StorageDescription;

    public Task<ImageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default) =>
        Resolve().UploadAsync(content, fileName, contentType, folder, cancellationToken);
}
