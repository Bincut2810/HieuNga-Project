using HieuNga.Domain.Entities;
using HieuNga.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Infrastructure.Persistence;

public class HieuNgaDbContext(DbContextOptions<HieuNgaDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Motorcycle> Motorcycles => Set<Motorcycle>();
    public DbSet<MotorcycleVariant> MotorcycleVariants => Set<MotorcycleVariant>();
    public DbSet<MotorcycleColor> MotorcycleColors => Set<MotorcycleColor>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<MaintenanceBooking> MaintenanceBookings => Set<MaintenanceBooking>();
    public DbSet<InstallmentRequest> InstallmentRequests => Set<InstallmentRequest>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(HieuNgaDbContext).Assembly);

        builder.Entity<ApplicationUser>().ToTable("admins");
        builder.Entity<IdentityRole<Guid>>().ToTable("admin_roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("admin_user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("admin_user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("admin_user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("admin_role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("admin_user_tokens");
    }
}
