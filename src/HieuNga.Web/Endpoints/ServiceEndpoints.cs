using HieuNga.Application.Media;
using HieuNga.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HieuNga.Web.Endpoints;

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

        group.MapPost("/images", async (Guid serviceId, HttpRequest request, IServiceCmsService cms, CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);
            var files = form.Files.Where(f => f.Length > 0).ToList();
            if (files.Count == 0)
                return Results.Json(new ServiceMutationResult(false, "Chưa chọn ảnh.", null));

            var uploads = new List<MediaFileUpload>();
            foreach (var file in files)
                uploads.Add(await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct));

            return Results.Json(await cms.UploadImagesAsync(serviceId, uploads, ct));
        });

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
}
