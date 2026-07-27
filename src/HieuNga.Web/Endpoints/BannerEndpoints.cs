using HieuNga.Application.Media;
using HieuNga.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HieuNga.Web.Endpoints;

public static class BannerEndpoints
{
    public static IEndpointRouteBuilder MapBannerApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/api/banner")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapGet("/", async (IBannerCmsService cms, CancellationToken ct) =>
            Results.Json(await cms.GetStateAsync(ct)));

        group.MapPost("/images", async (HttpRequest request, IBannerCmsService cms, CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);
            var files = form.Files.Where(f => f.Length > 0).ToList();
            if (files.Count == 0)
                return Results.Json(new BannerMutationResult(false, "Chưa chọn ảnh.", null));

            var uploads = new List<MediaFileUpload>();
            foreach (var file in files)
                uploads.Add(await MediaFileUploadAdapter.FromFormFileAsync(file, ct: ct));

            return Results.Json(await cms.UploadImagesAsync(uploads, ct));
        });

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
}
