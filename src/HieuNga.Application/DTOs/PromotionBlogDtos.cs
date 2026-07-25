using HieuNga.Domain.Enums;

namespace HieuNga.Application.DTOs;

public record PromotionDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string? Content,
    PromotionType Type,
    decimal? DiscountPercent,
    decimal? DiscountAmount,
    DateTime StartDate,
    DateTime EndDate,
    string? ImageUrl,
    string? MotorcycleName,
    string? MotorcycleSlug,
    SeoMetadataDto Seo);

public record BlogDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string Content,
    string? ThumbnailUrl,
    string? CategoryName,
    string? AuthorName,
    DateTime? PublishedAt,
    SeoMetadataDto Seo);

public record BlogCategoryDto(Guid Id, string Name, string Slug);

public record CreateConsultationDto(
    string CustomerName,
    string Phone,
    string? Email,
    string? Subject,
    string? Message,
    Guid? BranchId,
    Guid? MotorcycleId = null,
    string? LeadSource = null,
    string? Intent = null,
    string? XeSlug = null,
    string? ServiceSlug = null);
