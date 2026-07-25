using HieuNga.Domain.Enums;

namespace HieuNga.Application.DTOs;

public record SeoMetadataDto(
    string? Title,
    string? Description,
    string? Keywords,
    string? OgImageUrl,
    string? CanonicalUrl);

public record BannerDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string ImageUrl,
    string? MobileImageUrl,
    string? CtaText,
    string? CtaUrl);

public record PromotionDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    PromotionType Type,
    string? ImageUrl,
    DateTime EndDate);

public record BranchDto(
    Guid Id,
    string Name,
    string Address,
    string? Phone,
    string? Hotline,
    string? Email,
    string? MapEmbedUrl,
    string? OpeningHours,
    bool IsHeadOffice,
    string Slug = "");

public record ReviewDto(
    Guid Id,
    string CustomerName,
    int Rating,
    string? Title,
    string Content,
    string? MotorcycleName);

public record BlogPostListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string? ThumbnailUrl,
    DateTime? PublishedAt);

public record InstallmentCalculationDto(
    decimal VehiclePrice,
    decimal DownPayment,
    int TermMonths,
    decimal MonthlyPayment,
    decimal TotalPayment,
    decimal TotalInterest,
    decimal FinancedAmount = 0,
    string? BankName = null,
    decimal MonthlyRatePercent = 0);

public record CreateBookingDto(
    string CustomerName,
    string Phone,
    string? Email,
    DateTime PreferredDate,
    string? PreferredTime,
    string? Notes,
    Guid? MotorcycleId,
    Guid? BranchId);

public record CreateMaintenanceBookingDto(
    string CustomerName,
    string Phone,
    string? Email,
    string? MotorcycleModel,
    string? LicensePlate,
    string ServiceType,
    DateTime PreferredDate,
    string? PreferredTime,
    string? Notes,
    Guid? BranchId);

public record CreateInstallmentRequestDto(
    string CustomerName,
    string Phone,
    string? Email,
    Guid? MotorcycleId,
    decimal VehiclePrice,
    decimal DownPayment,
    int TermMonths,
    string? Notes);

public record HomepageDto(
    IReadOnlyList<BannerDto> HeroBanners,
    IReadOnlyList<MotorcycleListItemDto> FeaturedMotorcycles,
    IReadOnlyList<PromotionDto> Promotions,
    IReadOnlyList<BranchDto> Branches,
    IReadOnlyList<ReviewDto> Testimonials,
    IReadOnlyList<MotorcycleCategoryCountDto> CategoryCounts,
    IReadOnlyList<BlogPostListItemDto> LatestPosts,
    IReadOnlyList<FinanceBankDto> FinanceBanks,
    IReadOnlyList<ServiceItemListDto> Services);
