using HieuNga.Domain.Enums;

namespace HieuNga.Application.DTOs;

public record SeoMetadataDto(
    string? Title,
    string? Description,
    string? Keywords,
    string? OgImageUrl,
    string? CanonicalUrl);

public record HeroSlideDto(string ImageUrl);

public record HomepageHeroDto(
    string Title,
    string? Subtitle,
    bool Enabled,
    IReadOnlyList<HeroSlideDto> Slides);

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
    string MotorcycleModel,
    string ServiceType,
    DateTime PreferredDate,
    string PreferredTime,
    string? Notes);

public record MaintenanceBookingDto(
    Guid Id,
    string CustomerName,
    string Phone,
    string MotorcycleModel,
    string ServiceType,
    DateTime PreferredDate,
    string PreferredTime,
    string? Notes,
    BookingStatus Status,
    DateTime CreatedAt);

public record MaintenanceBoardDto(
    IReadOnlyList<MaintenanceBookingDto> Items,
    MaintenanceBoardCounts Counts);

public record MaintenanceBoardCounts(
    int Today,
    int Waiting,
    int CompletedToday,
    int Cancelled);

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
    HomepageHeroDto Hero,
    IReadOnlyList<MotorcycleListItemDto> FeaturedMotorcycles,
    IReadOnlyList<PromotionDto> Promotions,
    IReadOnlyList<BranchDto> Branches,
    IReadOnlyList<ReviewDto> Testimonials,
    IReadOnlyList<MotorcycleCategoryCountDto> CategoryCounts,
    IReadOnlyList<BlogPostListItemDto> LatestPosts,
    IReadOnlyList<FinanceBankDto> FinanceBanks,
    IReadOnlyList<ServiceItemListDto> Services);
