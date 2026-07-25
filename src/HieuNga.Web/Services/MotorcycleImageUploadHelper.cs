using System.Text.RegularExpressions;
using HieuNga.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HieuNga.Web.Services;

/// <summary>Motorcycle-scoped image uploads (Cloudinary / Local). Not a global Media Library.</summary>
public static class MotorcycleImageUploadHelper
{
    public static async Task<string?> TryUploadThumbnailAsync(
        IFormFile? file,
        IImageStorageService storage,
        ModelStateDictionary modelState,
        string fieldName = "ThumbnailFile",
        CancellationToken cancellationToken = default) =>
        await TryUploadAsync(file, storage, modelState, "motorcycles", fieldName, cancellationToken);

    public static async Task<string?> TryUploadAsync(
        IFormFile? file,
        IImageStorageService storage,
        ModelStateDictionary modelState,
        string folder,
        string fieldName = "",
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return null;

        if (!storage.SupportsUpload)
        {
            modelState.AddModelError(fieldName,
                "Upload file không khả dụng. Cấu hình Cloudinary (Production) hoặc Local (Development).");
            return null;
        }

        await using var stream = file.OpenReadStream();
        var result = await storage.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            folder,
            cancellationToken);

        if (!result.Success)
        {
            modelState.AddModelError(fieldName, result.ErrorMessage ?? "Không thể tải ảnh lên.");
            return null;
        }

        return result.PublicUrl;
    }

    /// <summary>Extract frame number from names like frame001, 001, spin_12.</summary>
    public static int? TryParseFrameNumber(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(name)) return null;
        var match = Regex.Match(name, @"(\d+)(?!.*\d)");
        if (!match.Success) return null;
        return int.TryParse(match.Groups[1].Value, out var n) ? n : null;
    }

    public static IReadOnlyList<int> FindMissingFrameIndices(IEnumerable<int> indices)
    {
        var sorted = indices.Distinct().OrderBy(i => i).ToList();
        if (sorted.Count == 0) return [];
        var missing = new List<int>();
        for (var i = sorted[0]; i <= sorted[^1]; i++)
        {
            if (!sorted.Contains(i)) missing.Add(i);
        }
        return missing;
    }
}
