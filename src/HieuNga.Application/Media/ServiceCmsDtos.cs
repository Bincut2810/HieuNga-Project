namespace HieuNga.Application.Media;

public record ServiceCmsImageDto(int Index, string Url);

public record ServiceCmsStateDto(
    Guid Id,
    string Name,
    string? ShortDescription,
    int DisplayOrder,
    bool Enabled,
    IReadOnlyList<ServiceCmsImageDto> Images);

public record ServiceMutationResult(bool Success, string? Message, ServiceCmsStateDto? State);
