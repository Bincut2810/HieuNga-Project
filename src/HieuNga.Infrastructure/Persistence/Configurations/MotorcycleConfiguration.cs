using HieuNga.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HieuNga.Infrastructure.Persistence.Configurations;

public class MotorcycleConfiguration : IEntityTypeConfiguration<Motorcycle>
{
    public void Configure(EntityTypeBuilder<Motorcycle> builder)
    {
        builder.ToTable("motorcycles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BasePrice).HasPrecision(18, 0);
        builder.Property(x => x.HighlightsJson).HasColumnType("text");
        builder.Property(x => x.TechnicalSpecsJson).HasColumnType("text");
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.Variants).WithOne(x => x.Motorcycle).HasForeignKey(x => x.MotorcycleId);
        builder.HasMany(x => x.Colors).WithOne(x => x.Motorcycle).HasForeignKey(x => x.MotorcycleId);
        builder.HasMany(x => x.MediaAssets).WithOne(x => x.Motorcycle).HasForeignKey(x => x.MotorcycleId);
        builder.HasMany(x => x.Features).WithOne(x => x.Motorcycle).HasForeignKey(x => x.MotorcycleId);
        builder.HasMany(x => x.Technologies).WithOne(x => x.Motorcycle).HasForeignKey(x => x.MotorcycleId);
        builder.HasMany(x => x.SpinFrames).WithOne(x => x.Motorcycle).HasForeignKey(x => x.MotorcycleId);
        builder.HasMany(x => x.Reviews).WithOne(x => x.Motorcycle).HasForeignKey(x => x.MotorcycleId);
    }
}
