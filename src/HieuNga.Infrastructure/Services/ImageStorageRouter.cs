using HieuNga.Application.Interfaces;
using HieuNga.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HieuNga.Infrastructure.Services;

public sealed class ImageStorageRouter(
    IServiceProvider services,
    IOptions<ImageStorageOptions> options,
    IHostEnvironment environment) : IImageStorageService
{
    private IImageStorageService Resolve()
    {
        var cfg = options.Value;
        if (cfg.UseCloudinary && cfg.Cloudinary.IsConfigured)
            return services.GetRequiredService<CloudinaryImageStorageService>();

        if (!environment.IsDevelopment() && cfg.UseCloudinary && !cfg.Cloudinary.IsConfigured)
            return services.GetRequiredService<DisabledImageStorageService>();

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

/// <summary>Used in staging/production when Cloudinary is selected but not configured.</summary>
public sealed class DisabledImageStorageService : IImageStorageService
{
    public bool SupportsUpload => false;

    public string StorageDescription =>
        "Upload tạm thời tắt trên môi trường này. Dùng URL ảnh hoặc cấu hình Cloudinary (ImageStorage__Provider=Cloudinary).";

    public Task<ImageUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ImageUploadResult.Fail(
            "Upload file bị tắt trên staging/production khi chưa cấu hình Cloudinary. Nhập URL ảnh thay thế."));
}
