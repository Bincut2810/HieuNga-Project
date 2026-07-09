using HieuNga.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HieuNga.Web.Services;

public static class MotorcycleImageUploadHelper
{
    public static async Task<string?> TryUploadThumbnailAsync(
        IFormFile? file,
        IImageStorageService storage,
        ModelStateDictionary modelState,
        string fieldName = "Input.ThumbnailFile",
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return null;

        if (!storage.SupportsUpload)
        {
            modelState.AddModelError(fieldName,
                "Upload file không khả dụng trên môi trường này. Nhập URL ảnh hoặc cấu hình Cloudinary.");
            return null;
        }

        await using var stream = file.OpenReadStream();
        var result = await storage.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            "motorcycles",
            cancellationToken);

        if (!result.Success)
        {
            modelState.AddModelError(fieldName, result.ErrorMessage ?? "Không thể tải ảnh lên.");
            return null;
        }

        return result.PublicUrl;
    }
}
