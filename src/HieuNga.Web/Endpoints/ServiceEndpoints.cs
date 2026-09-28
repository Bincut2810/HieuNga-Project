using HieuNga.Application.Media;
using Microsoft.AspNetCore.Mvc;

namespace HieuNga.Web.Endpoints;

/// <summary>
/// Service CMS endpoints (per-service gallery management). The actual file
/// upload is performed by the canonical <see cref="ImageUploadEndpoints"/>;
/// this surface receives JSON bodies with already-uploaded URLs and performs
/// the database operations (append, delete by index, reorder by index,
/// save shared settings).
/// </summary>
public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/dich-vu/{serviceId:guid}/media")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapGet("/", async (Guid serviceId, IServiceCmsService cms, CancellationToken ct) =>
        {
            var state = await cms.GetStateAsync(serviceId, ct);
            return state is null ? Results.NotFound() : Results.Json(state);
        });

        group.MapPost("/images", async (Guid serviceId, [FromBody] AddImagesBody body, IServiceCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.AddImagesAsync(serviceId, body.Urls ?? [], ct)));

        group.MapDelete("/images/{index:int}", async (Guid serviceId, int index, IServiceCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.DeleteImageAsync(serviceId, index, ct)));

        group.MapPost("/reorder", async (Guid serviceId, [FromBody] OrderBody body, IServiceCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.ReorderImagesAsync(serviceId, body.Indexes ?? [], ct)));

        group.MapPost("/settings", async (Guid serviceId, [FromBody] SettingsBody body, IServiceCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.SaveSettingsAsync(
                serviceId,
                body.Name ?? "",
                body.ShortDescription,
                body.DisplayOrder,
                body.Enabled,
                ct)));

        return app;
    }

    public sealed record OrderBody(List<int>? Indexes);
    public sealed record SettingsBody(string? Name, string? ShortDescription, int DisplayOrder, bool Enabled);
    public sealed record AddImagesBody(List<string>? Urls);
}
