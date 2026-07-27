namespace HieuNga.Application.Media;

public interface IBannerCmsService
{
    Task<BannerCmsStateDto> GetStateAsync(CancellationToken ct = default);
    Task<BannerMutationResult> UploadImagesAsync(IReadOnlyList<MediaFileUpload> files, CancellationToken ct = default);
    Task<BannerMutationResult> DeleteImageAsync(Guid id, CancellationToken ct = default);
    Task<BannerMutationResult> ReorderImagesAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);
    Task<BannerMutationResult> SaveSettingsAsync(string title, string? subtitle, bool enabled, CancellationToken ct = default);
}
