using FluentValidation;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HieuNga.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHomepageService, HomepageService>();
        services.AddScoped<IMotorcycleService, MotorcycleService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
