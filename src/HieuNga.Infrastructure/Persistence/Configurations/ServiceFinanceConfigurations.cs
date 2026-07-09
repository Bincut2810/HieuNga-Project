using HieuNga.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HieuNga.Infrastructure.Persistence.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("service_categories");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> builder)
    {
        builder.ToTable("service_items");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(x => x.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BankTypeConfiguration : IEntityTypeConfiguration<BankType>
{
    public void Configure(EntityTypeBuilder<BankType> builder)
    {
        builder.ToTable("bank_types");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.ToTable("banks");
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.BankType)
            .WithMany(t => t.Banks)
            .HasForeignKey(x => x.BankTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FinanceRateConfiguration : IEntityTypeConfiguration<FinanceRate>
{
    public void Configure(EntityTypeBuilder<FinanceRate> builder)
    {
        builder.ToTable("finance_rates");
        builder.Property(x => x.MonthlyInterestRatePercent).HasPrecision(8, 4);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.Bank)
            .WithMany(b => b.FinanceRates)
            .HasForeignKey(x => x.BankId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
