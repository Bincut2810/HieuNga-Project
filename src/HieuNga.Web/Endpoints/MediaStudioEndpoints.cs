using HieuNga.Application.Media;
using HieuNga.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HieuNga.Web.Endpoints;

/// <summary>
/// Motorcycle media CRUD endpoints. Image upload is performed by the canonical
/// <see cref="ImageUploadEndpoints"/>; this surface receives JSON bodies
/// containing the already-uploaded URL and performs the database operations
/// (state read, slot assignments, color CRUD, angle CRUD, reorder).
///
/// All request bodies use a JSON envelope: <c>{ url }</c> for a single
/// assignment, or <c>{ urls }</c> for batched assignments.
/// </summary>
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

        group.MapPost("/thumbnail", async (
            Guid motorcycleId,
            [FromBody] UrlBody body,
            IMotorcycleMediaStudioService media,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body?.Url))
                return Results.Json(new MediaMutationResult(false, "Thiếu URL ảnh.", null));
            return Results.Json(await media.SetSlotUrlAsync(motorcycleId, MediaSlot.Thumbnail, body.Url, ct));
        });

        group.MapDelete("/thumbnail", async (Guid motorcycleId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ClearSlotAsync(motorcycleId, MediaSlot.Thumbnail, ct)));

        group.MapPost("/colors", async (
            Guid motorcycleId,
            [FromBody] UpsertColorBody body,
            IMotorcycleMediaStudioService media,
            CancellationToken ct) =>
        {
            return Results.Json(await media.UpsertColorUrlAsync(
                motorcycleId,
                body?.ColorId,
                body?.Name ?? "",
                body?.Hex ?? "",
                string.IsNullOrWhiteSpace(body?.ImageUrl) ? null : body!.ImageUrl,
                ct));
        });

        group.MapPost("/colors/{colorId:guid}/image", async (
            Guid motorcycleId,
            Guid colorId,
            [FromBody] UrlBody body,
            IMotorcycleMediaStudioService media,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body?.Url))
                return Results.Json(new MediaMutationResult(false, "Thiếu URL ảnh.", null));
            return Results.Json(await media.ReplaceColorImageUrlAsync(motorcycleId, colorId, body.Url, ct));
        });

        group.MapPost("/colors/reorder", async (
            Guid motorcycleId,
            [FromBody] OrderBody body,
            IMotorcycleMediaStudioService media,
            CancellationToken ct) =>
            Results.Json(await media.ReorderColorsAsync(motorcycleId, body?.Ids ?? [], ct)));

        group.MapDelete("/colors/{colorId:guid}", async (Guid motorcycleId, Guid colorId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.DeleteColorAsync(motorcycleId, colorId, ct)));

        group.MapPost("/angles/{angleKey}", async (
            Guid motorcycleId,
            string angleKey,
            [FromBody] UrlBody body,
            IMotorcycleMediaStudioService media,
            CancellationToken ct) =>
        {
            if (!MotorcycleViewAngleCatalog.TryParseKey(angleKey, out var angle))
                return Results.Json(new MediaMutationResult(false, $"Góc xem không hợp lệ: {angleKey}", null));
            if (string.IsNullOrWhiteSpace(body?.Url))
                return Results.Json(new MediaMutationResult(false, "Thiếu URL ảnh.", null));
            return Results.Json(await media.SetAngleUrlAsync(motorcycleId, angle, body.Url, ct));
        });

        group.MapDelete("/angles/{angleKey}", async (
            Guid motorcycleId,
            string angleKey,
            IMotorcycleMediaStudioService media,
            CancellationToken ct) =>
        {
            if (!MotorcycleViewAngleCatalog.TryParseKey(angleKey, out var angle))
                return Results.Json(new MediaMutationResult(false, $"Góc xem không hợp lệ: {angleKey}", null));
            return Results.Json(await media.ClearAngleAsync(motorcycleId, angle, ct));
        });

        group.MapDelete("/angles", async (Guid motorcycleId, IMotorcycleMediaStudioService media, CancellationToken ct) =>
            Results.Json(await media.ClearAllAnglesAsync(motorcycleId, ct)));

        return app;
    }

    public sealed record OrderBody(List<Guid>? Ids);
    public sealed record UrlBody(string? Url);
    public sealed record UpsertColorBody(Guid? ColorId, string? Name, string? Hex, string? ImageUrl);
}
