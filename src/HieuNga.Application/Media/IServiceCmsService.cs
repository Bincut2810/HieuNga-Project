namespace HieuNga.Application.Media;

public interface IServiceCmsService
{
    Task<ServiceCmsStateDto?> GetStateAsync(Guid serviceId, CancellationToken ct = default);

    /// <summary>Append already-uploaded image URLs to the service gallery.</summary>
    Task<ServiceMutationResult> AddImagesAsync(Guid serviceId, IReadOnlyList<string> urls, CancellationToken ct = default);

    Task<ServiceMutationResult> DeleteImageAsync(Guid serviceId, int index, CancellationToken ct = default);
    Task<ServiceMutationResult> ReorderImagesAsync(Guid serviceId, IReadOnlyList<int> orderedIndexes, CancellationToken ct = default);
    Task<ServiceMutationResult> SaveSettingsAsync(
        Guid serviceId,
        string name,
        string? shortDescription,
        int displayOrder,
        bool enabled,
        CancellationToken ct = default);
}
