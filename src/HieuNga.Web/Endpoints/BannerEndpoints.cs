using HieuNga.Application.Media;
using Microsoft.AspNetCore.Mvc;

namespace HieuNga.Web.Endpoints;

/// <summary>
/// Banner CMS endpoints. The actual file upload is performed by the canonical
/// <see cref="ImageUploadEndpoints"/>; this surface only receives a JSON body
/// with the already-uploaded URLs and performs the database operations
/// (add rows, soft-delete, reorder, save shared settings).
/// </summary>
public static class BannerEndpoints
{
    public static IEndpointRouteBuilder MapBannerApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/banner")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapGet("/", async (IBannerCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.GetStateAsync(ct)));

        group.MapPost("/images", async ([FromBody] AddImagesBody body, IBannerCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.AddImagesAsync(body.Urls ?? [], ct)));

        group.MapDelete("/images/{id:guid}", async (Guid id, IBannerCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.DeleteImageAsync(id, ct)));

        group.MapPost("/reorder", async ([FromBody] OrderBody body, IBannerCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.ReorderImagesAsync(body.Ids ?? [], ct)));

        group.MapPost("/settings", async ([FromBody] SettingsBody body, IBannerCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.SaveSettingsAsync(body.Title ?? "", body.Subtitle, body.Enabled, ct)));

        return app;
    }

    public sealed record OrderBody(List<Guid>? Ids);
    public sealed record SettingsBody(string? Title, string? Subtitle, bool Enabled);
    public sealed record AddImagesBody(List<string>? Urls);
}
