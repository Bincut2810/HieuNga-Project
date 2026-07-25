using HieuNga.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HieuNga.Infrastructure.Persistence.Configurations;

public class MotorcycleVariantConfiguration : IEntityTypeConfiguration<MotorcycleVariant>
{
    public void Configure(EntityTypeBuilder<MotorcycleVariant> builder)
    {
        builder.ToTable("motorcycle_variants");
        builder.Property(x => x.Price).HasPrecision(18, 0);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MotorcycleColorConfiguration : IEntityTypeConfiguration<MotorcycleColor>
{
    public void Configure(EntityTypeBuilder<MotorcycleColor> builder)
    {
        builder.ToTable("motorcycle_colors");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MotorcycleFeatureConfiguration : IEntityTypeConfiguration<MotorcycleFeature>
{
    public void Configure(EntityTypeBuilder<MotorcycleFeature> builder)
    {
        builder.ToTable("motorcycle_features");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MotorcycleTechnologyConfiguration : IEntityTypeConfiguration<MotorcycleTechnology>
{
    public void Configure(EntityTypeBuilder<MotorcycleTechnology> builder)
    {
        builder.ToTable("motorcycle_technologies");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MotorcycleSpinFrameConfiguration : IEntityTypeConfiguration<MotorcycleSpinFrame>
{
    public void Configure(EntityTypeBuilder<MotorcycleSpinFrame> builder)
    {
        builder.ToTable("motorcycle_spin_frames");
        builder.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.MotorcycleId, x.FrameIndex });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder) => builder.ToTable("bookings");
}

public class MaintenanceBookingConfiguration : IEntityTypeConfiguration<MaintenanceBooking>
{
    public void Configure(EntityTypeBuilder<MaintenanceBooking> builder) => builder.ToTable("maintenance_bookings");
}

public class InstallmentRequestConfiguration : IEntityTypeConfiguration<InstallmentRequest>
{
    public void Configure(EntityTypeBuilder<InstallmentRequest> builder) => builder.ToTable("installment_requests");
}

public class BlogCategoryConfiguration : IEntityTypeConfiguration<BlogCategory>
{
    public void Configure(EntityTypeBuilder<BlogCategory> builder)
    {
        builder.ToTable("blog_categories");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("blog_posts");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder) => builder.ToTable("banners");
}

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("site_settings");
        builder.HasIndex(x => x.Key).IsUnique();
    }
}
