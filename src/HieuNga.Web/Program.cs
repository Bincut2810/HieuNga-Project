using System.Net;
using HieuNga.Application;
using HieuNga.Infrastructure;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Middleware;
using Microsoft.AspNetCore.HttpOverrides;

// Render.com injects PORT — bind Kestrel before CreateBuilder finishes URL config
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{renderPort}");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/DangNhap");
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/dang-nhap";
    options.AccessDeniedPath = "/admin/dang-nhap";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HieuNga.Web.Services.CompareSessionService>();
builder.Services.AddResponseCompression();
builder.Services.AddAntiforgery();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SeoMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));
app.MapRazorPages();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
try
{
    using var scope = app.Services.CreateScope();
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
    logger.LogInformation("Database initialization completed.");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Database initialization failed. Check ConnectionStrings__DefaultConnection.");
    throw;
}

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5000";
logger.LogInformation("Honda Hiếu Nga starting. Environment={Env} URLs={Urls}",
    app.Environment.EnvironmentName, urls);

app.Run();
