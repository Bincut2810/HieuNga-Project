using HieuNga.Domain.Enums;

namespace HieuNga.Application.DTOs;

public record MotorcycleListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    MotorcycleCategory Category,
    decimal BasePrice,
    string? ThumbnailUrl,
    bool IsFeatured,
    bool IsAvailable = true,
    string AvailabilityLabel = "Còn hàng");

public record MotorcycleSpecItemDto(string Icon, string Label, string Value);

public record MotorcycleDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    MotorcycleCategory Category,
    decimal BasePrice,
    int? EngineCc,
    string? FuelType,
    string? Transmission,
    string? ThumbnailUrl,
    IReadOnlyList<MotorcycleVariantDto> Variants,
    IReadOnlyList<MotorcycleColorDto> Colors,
    IReadOnlyList<string> GalleryUrls,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<MotorcycleSpecItemDto> Specifications,
    IReadOnlyList<MotorcycleFeatureDto> Features,
    IReadOnlyList<MotorcycleTechnologyDto> Technologies,
    IReadOnlyList<string> SpinFrameUrls,
    SeoMetadataDto Seo);

public record MotorcycleVariantDto(Guid Id, string Name, decimal Price, int StockQuantity, bool IsAvailable);
public record MotorcycleColorDto(Guid Id, string Name, string HexCode, string? ImageUrl);
public record MotorcycleFeatureDto(Guid Id, string Title, string? Description, string ImageUrl, int SortOrder);
public record MotorcycleTechnologyDto(Guid Id, string Title, string? Description, string ImageUrl, int SortOrder);

public record MotorcycleFilterDto(
    string? Query,
    MotorcycleCategory? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page = 1,
    int PageSize = 12,
    bool? FeaturedOnly = null,
    string? Sort = null);

public record MotorcycleCategoryCountDto(MotorcycleCategory Category, string Label, int Count);

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
