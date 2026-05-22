using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Identity;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HieuNga.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
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

        return services;
    }
}
