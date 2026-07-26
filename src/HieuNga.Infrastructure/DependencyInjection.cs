using HieuNga.Application.DemoImport;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Media;
using HieuNga.Application.Options;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Identity;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Repositories;
using HieuNga.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HieuNga.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));
        services.Configure<ImageStorageOptions>(configuration.GetSection(ImageStorageOptions.SectionName));

        // AdminSeed__Enabled alias for SeedOptions__AdminSeedEnabled
        var adminSeedEnabled = configuration["AdminSeed:Enabled"] ?? configuration["SeedOptions:AdminSeedEnabled"];
        if (!string.IsNullOrWhiteSpace(adminSeedEnabled))
        {
            services.PostConfigure<SeedOptions>(o => o.AdminSeedEnabled = bool.TryParse(adminSeedEnabled, out var enabled) && enabled);
        }

        // AdminSeed__Email / AdminSeed__Password aliases
        var adminEmail = configuration["AdminSeed:Email"];
        var adminPassword = configuration["AdminSeed:Password"];
        if (!string.IsNullOrWhiteSpace(adminEmail) || !string.IsNullOrWhiteSpace(adminPassword))
        {
            services.PostConfigure<SeedOptions>(o =>
            {
                if (!string.IsNullOrWhiteSpace(adminEmail)) o.AdminEmail = adminEmail;
                if (!string.IsNullOrWhiteSpace(adminPassword)) o.AdminPassword = adminPassword;
            });
        }

        services.AddDbContext<HieuNgaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<HieuNgaDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IMotorcycleRepository, MotorcycleRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IBannerRepository, BannerRepository>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IFinanceConfigService, FinanceConfigService>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();

        services.AddSingleton<LocalImageStorageService>();
        services.AddSingleton<CloudinaryImageStorageService>();
        services.AddSingleton<DisabledImageStorageService>();
        services.AddSingleton<IImageStorageService, ImageStorageRouter>();
        services.AddScoped<IMotorcycleMediaStudioService, MotorcycleMediaStudioService>();
        services.AddScoped<IDemoMotorcycleImporter, DemoMotorcycleImporter>();

        return services;
    }
}
