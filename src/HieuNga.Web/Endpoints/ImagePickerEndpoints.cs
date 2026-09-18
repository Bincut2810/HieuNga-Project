using HieuNga.Application.Media;
using HieuNga.Web.Services;

namespace HieuNga.Web.Endpoints;

/// <summary>
/// Generic single-image uploader for forms that only need ONE image
/// (Promotions, BlogPosts, etc.). Reuses the existing IMotorcycleMediaStudioService
/// so we benefit from the same Cloudinary / Local validation pipeline as Motorcycles.
/// </summary>
public static class ImagePickerEndpoints
{
    public static IEndpointRouteBuilder MapImagePickerApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/image-upload")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapPost("/", async (
            HttpRequest request,
            IMotorcycleMediaStudioService media,
            CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);

            // Folder on storage — e.g. "promotions", "blog".
            var kind = (form["kind"].ToString() ?? "").Trim().ToLowerInvariant();
            var folder = ResolveFolder(kind);
            if (folder is null)
                return Results.Json(new ImageUploadResult(false, null, $"Loại ảnh không hợp lệ: {kind}"));

            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is null || file.Length <= 0)
                return Results.Json(new ImageUploadResult(false, null, "Chưa chọn ảnh."));

            var upload = await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct);
            var result = await media.UploadOnlyAsync(upload, folder, ct);
            return result.Ok
                ? Results.Json(new ImageUploadResult(true, result.Url, null))
                : Results.Json(new ImageUploadResult(false, null, result.Error ?? "Không tải được ảnh."));
        });

        return app;
    }

    private static string? ResolveFolder(string kind) => kind switch
    {
        "promotion" or "promotions" or "khuyen-mai" => "promotions",
        "blog" or "blogpost" or "blogposts" or "tin-tuc" => "blog",
        "bank" or "banks" or "ngan-hang" or "nganhang" => "banks",
        _ => null
    };
}

public sealed record ImageUploadResult(bool Ok, string? Url, string? Message);