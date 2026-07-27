using HieuNga.Application.Media;
using HieuNga.Domain;
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

        group.MapPost("/angles/{angleKey}", async (Guid motorcycleId, string angleKey, IFormFile file, IMotorcycleMediaStudioService media, CancellationToken ct) =>
        {
            if (!MotorcycleViewAngleCatalog.TryParseKey(angleKey, out var angle))
                return Results.Json(new MediaMutationResult(false, $"Góc xem không hợp lệ: {angleKey}", null));
            return Results.Json(await media.SetAngleAsync(motorcycleId, angle, await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct), ct));
        });

        group.MapDelete("/angles/{angleKey}", async (Guid motorcycleId, string angleKey, IMotorcycleMediaStudioService media, CancellationToken ct) =>
        {
            if (!MotorcycleViewAngleCatalog.TryParseKey(angleKey, out var angle))
                return Results.Json(new MediaMutationResult(false, $"Góc xem không hợp lệ: {angleKey}", null));
            return Results.Json(await media.ClearAngleAsync(motorcycleId, angle, ct));
        });

        group.MapDelete("/angles", async (Guid motorcycleId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ClearAllAnglesAsync(motorcycleId, ct)));

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
                uploads.Add(await MediaFileUploadAdapter.FromFormFileAsync(f, rel, ct));
            }

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

    public sealed record OrderBody(List<Guid>? Ids);
}
