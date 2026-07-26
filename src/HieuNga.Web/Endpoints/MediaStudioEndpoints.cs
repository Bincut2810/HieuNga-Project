using HieuNga.Application.Media;
using HieuNga.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HieuNga.Web.Endpoints;

public static class MediaStudioEndpoints
{
    public static IEndpointRouteBuilder MapMediaStudioApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/xe/{motorcycleId:guid}/media")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapGet("/", async (Guid motorcycleId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
        {
            var state = await media.GetStateAsync(motorcycleId, ct);
            return state is null ? Results.NotFound() : Results.Json(state);
        });

        group.MapPost("/thumbnail", async (Guid motorcycleId, IFormFile file, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.SetSlotAsync(motorcycleId, MediaSlot.Thumbnail, await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct), ct)));

        group.MapDelete("/thumbnail", async (Guid motorcycleId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ClearSlotAsync(motorcycleId, MediaSlot.Thumbnail, ct)));

        group.MapPost("/hero", async (Guid motorcycleId, IFormFile file, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.SetSlotAsync(motorcycleId, MediaSlot.Hero, await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct), ct)));

        group.MapDelete("/hero", async (Guid motorcycleId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ClearSlotAsync(motorcycleId, MediaSlot.Hero, ct)));

        group.MapPost("/gallery", async (Guid motorcycleId, [FromForm] List<IFormFile> files, IMotorcycleMediaStudioService media, CancellationToken ct) =>
        {
            var uploads = await MediaFileUploadAdapter.FromFormFilesAsync(files ?? [], ct);
            return Results.Json(await media.AddGalleryAsync(motorcycleId, uploads, ct));
        });

        group.MapPost("/gallery/{mediaId:guid}/replace", async (Guid motorcycleId, Guid mediaId, IFormFile file, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ReplaceGalleryAsync(motorcycleId, mediaId, await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct), ct)));

        group.MapPost("/gallery/{mediaId:guid}/caption", async (Guid motorcycleId, Guid mediaId, [FromBody] CaptionBody body, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.UpdateGalleryCaptionAsync(motorcycleId, mediaId, body.Caption, ct)));

        group.MapPost("/gallery/reorder", async (Guid motorcycleId, [FromBody] OrderBody body, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ReorderGalleryAsync(motorcycleId, body.Ids ?? [], ct)));

        group.MapPost("/gallery/delete", async (Guid motorcycleId, [FromBody] IdsBody body, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.DeleteGalleryAsync(motorcycleId, body.Ids ?? [], ct)));

        group.MapPost("/colors", async (Guid motorcycleId, [FromForm] string name, [FromForm] string hex, IFormFile? image, Guid? colorId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
        {
            MediaFileUpload? upload = image is { Length: > 0 } ? await MediaFileUploadAdapter.FromFormFileAsync(image, ct: ct) : null;
            return Results.Json(await media.UpsertColorAsync(motorcycleId, colorId, name ?? "", hex ?? "", upload, ct));
        });

        group.MapPost("/colors/{colorId:guid}/image", async (Guid motorcycleId, Guid colorId, IFormFile file, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ReplaceColorImageAsync(motorcycleId, colorId, await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct), ct)));

        group.MapPost("/colors/reorder", async (Guid motorcycleId, [FromBody] OrderBody body, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ReorderColorsAsync(motorcycleId, body.Ids ?? [], ct)));

        group.MapDelete("/colors/{colorId:guid}", async (Guid motorcycleId, Guid colorId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.DeleteColorAsync(motorcycleId, colorId, ct)));

        group.MapPost("/spin", async (Guid motorcycleId, [FromForm] List<IFormFile> files, IMotorcycleMediaStudioService media, CancellationToken ct) =>
        {
            var uploads = await MediaFileUploadAdapter.FromFormFilesAsync(files ?? [], ct);
            return Results.Json(await media.UploadSpinAsync(motorcycleId, uploads, ct));
        });

        group.MapPost("/spin/reorder", async (Guid motorcycleId, [FromBody] OrderBody body, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ReorderSpinAsync(motorcycleId, body.Ids ?? [], ct)));

        group.MapPost("/spin/delete", async (Guid motorcycleId, [FromBody] IdsBody body, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.DeleteSpinAsync(motorcycleId, body.Ids ?? [], ct)));

        group.MapDelete("/spin", async (Guid motorcycleId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ClearSpinAsync(motorcycleId, ct)));

        group.MapPost("/import", async (Guid motorcycleId, HttpRequest request, IMotorcycleMediaStudioService media, CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);
            var files = form.Files;
            var uploads = new List<MediaFileUpload>();
            foreach (var f in files.Where(x => x.Length > 0))
            {
                var rel = form[$"path:{f.Name}"].FirstOrDefault()
                    ?? form[$"path_{f.FileName}"].FirstOrDefault()
                    ?? f.FileName;
                // Prefer webkit-style name field if provided as paths[] parallel
                uploads.Add(await MediaFileUploadAdapter.FromFormFileAsync(f, rel, ct));
            }

            // Also accept paths[] matching files order
            var paths = form["paths"].ToList();
            if (paths.Count == uploads.Count)
            {
                for (var i = 0; i < uploads.Count; i++)
                {
                    uploads[i] = new MediaFileUpload
                    {
                        Content = uploads[i].Content,
                        FileName = uploads[i].FileName,
                        ContentType = uploads[i].ContentType,
                        Length = uploads[i].Length,
                        RelativePath = paths[i]
                    };
                }
            }

            return Results.Json(await media.SmartImportAsync(motorcycleId, uploads, ct));
        });

        return app;
    }

    public sealed record CaptionBody(string? Caption);
    public sealed record OrderBody(List<Guid>? Ids);
    public sealed record IdsBody(List<Guid>? Ids);
}
