namespace HieuNga.Application.Media;

public record BannerImageDto(Guid Id, string Url, int DisplayOrder);

public record BannerCmsStateDto(
    string Title,
    string? Subtitle,
    bool Enabled,
    IReadOnlyList<BannerImageDto> Images);

public record BannerMutationResult(bool Success, string? Message, BannerCmsStateDto? State);
