using HieuNga.Application;
using HieuNga.Application.Options;
using HieuNga.Infrastructure;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Web.Endpoints;
using HieuNga.Web.Filters;
using HieuNga.Web.Middleware;
using HieuNga.Web.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;

// Render.com injects PORT — bind Kestrel before CreateBuilder finishes URL config
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{renderPort}");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/dang-nhap";
    options.AccessDeniedPath = "/admin/dang-nhap";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CompareSessionService>();
builder.Services.AddResponseCompression();
builder.Services.AddAntiforgery();
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 105_000_000; // ~100 MB media uploads
});
builder.Services.AddScoped<SiteSettingsPageFilter>();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/DangNhap");
}).AddMvcOptions(o => o.Filters.Add<SiteSettingsPageFilter>());

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

app.MapGet("/health", async (HieuNgaDbContext db, IHostEnvironment env, CancellationToken ct) =>
{
    var dbConnected = false;
    try
    {
        dbConnected = await db.Database.CanConnectAsync(ct);
    }
    catch
    {
        dbConnected = false;
    }

    var payload = new
    {
        status = dbConnected ? "Healthy" : "Unhealthy",
        database = dbConnected ? "Connected" : "Disconnected",
        environment = env.EnvironmentName,
        timestamp = DateTime.UtcNow
    };

    return dbConnected
        ? Results.Json(payload)
        : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapMediaStudioApi();
app.MapBannerApi();
app.MapServiceApi();
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
logger.LogInformation("Xe Máy Hiếu Nga starting. Environment={Env} URLs={Urls}",
    app.Environment.EnvironmentName, urls);

app.Run();
