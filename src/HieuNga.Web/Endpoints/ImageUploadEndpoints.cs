using HieuNga.Application.Media;
using Microsoft.AspNetCore.Http;

namespace HieuNga.Web.Endpoints;

/// <summary>
/// Canonical, single-purpose image-upload endpoint for the admin panel.
///
/// <para>Route: <c>POST /admin/api/upload</c> (multipart/form-data).</para>
/// <para>
/// Request fields:
///   • <c>file</c> — required image file. The widget always sends this name.
///   • <c>kind</c> — required logical kind (see <see cref="ImageUploadKinds"/>).
///   • <c>contextId</c> — optional <see cref="Guid"/>. Used by kinds whose storage
///     folder depends on an entity (motorcycle-* / service-gallery).
/// </para>
/// <para>
/// Response: <see cref="ImageUploadResponse"/>. The endpoint selects an HTTP status
/// code based on the response's <c>code</c> field, so the frontend can branch on
/// either field without parsing messages.
/// </para>
/// <para>
/// Domain endpoints (motorcycle media, banner CMS, service CMS) still accept
/// multipart uploads in their own routes for backward compatibility, but they
/// delegate to <see cref="IImageUploadService"/> for validation/storage.
/// </para>
/// </summary>
public static class ImageUploadEndpoints
{
    public static IEndpointRouteBuilder MapImageUploadApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/upload")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapPost("/", HandleUploadAsync);

        return app;
    }

    private static async Task<IResult> HandleUploadAsync(
        HttpRequest request,
        IImageUploadService uploader,
        CancellationToken ct)
    {
        // We read the form manually so we can pick out the file without binding to
        // a specific IFormFile parameter (Razor Pages' multipart binder has different
        // semantics than Minimal APIs).
        var form = await request.ReadFormAsync(ct);

        var kind = (form["kind"].ToString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(kind))
            return Json(ImageUploadResponse.Failure(
                ImageUploadErrorCodes.InvalidFile,
                "Thiếu trường 'kind'."), StatusCodes.Status400BadRequest);

        Guid? contextId = null;
        var contextRaw = form["contextId"].ToString();
        if (!string.IsNullOrWhiteSpace(contextRaw))
        {
            if (!Guid.TryParse(contextRaw, out var parsed))
                return Json(ImageUploadResponse.Failure(
                    ImageUploadErrorCodes.InvalidFile,
                    "contextId không phải GUID hợp lệ."), StatusCodes.Status400BadRequest);
            contextId = parsed;
        }

        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null)
            return Json(ImageUploadResponse.Failure(
                ImageUploadErrorCodes.InvalidFile,
                "Thiếu trường 'file'."), StatusCodes.Status400BadRequest);

        // Copy the upload stream into a MemoryStream so the application service can
        // rewind it (some storage providers read the stream more than once).
        var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;
        var payload = new ImageUploadPayload
        {
            Content = ms,
            FileName = file.FileName ?? "upload",
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType!,
            Length = file.Length
        };

        var response = await uploader.UploadAsync(payload, kind, contextId, ct);
        var status = MapStatus(response.Code);
        return Json(response, status);
    }

    private static IResult Json(ImageUploadResponse body, int statusCode)
    {
        return Results.Json(body, statusCode: statusCode);
    }

    private static int MapStatus(string? code) => code switch
    {
        null => StatusCodes.Status200OK,
        ImageUploadErrorCodes.InvalidFile => StatusCodes.Status400BadRequest,
        ImageUploadErrorCodes.UnsupportedType => StatusCodes.Status400BadRequest,
        ImageUploadErrorCodes.FileTooLarge => StatusCodes.Status413PayloadTooLarge,
        ImageUploadErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
        ImageUploadErrorCodes.StorageUnavailable => StatusCodes.Status503ServiceUnavailable,
        ImageUploadErrorCodes.StorageUploadFailed => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError
    };
}
