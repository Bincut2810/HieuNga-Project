using System.Xml.Linq;
using HieuNga.Application.Options;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HieuNga.Web.Endpoints;

public static class SitemapEndpoints
{
    private const string DefaultBaseUrl = "https://xemayhieunga.com";
    private const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static readonly string[] StaticPaths =
    [
        "/",
        "/xe",
        "/dich-vu",
        "/bao-duong",
        "/tin-tuc",
        "/khuyen-mai",
        "/lien-he",
        "/so-sanh",
        "/dat-lich-lai-thu"
    ];

    public static IEndpointRouteBuilder MapSitemap(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitemap.xml", async (
            HieuNgaDbContext db,
            IOptions<SiteOptions> siteOptions,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("HieuNga.Sitemap");
            var baseUrl = ResolveBaseUrl(siteOptions.Value.BaseUrl);

            var entries = new List<SitemapEntry>();

            // 1. Static public URLs
            foreach (var path in StaticPaths)
                entries.Add(new SitemapEntry(BuildLoc(baseUrl, path), null));

            try
            {
                // 2. Motorcycles — reuse visibility rule from MotorcycleRepository.GetBySlugAsync
                //    (IsPublished && !IsDeleted).
                var motorcycles = await db.Motorcycles.AsNoTracking()
                    .Where(m => m.IsPublished && !m.IsDeleted
                                && !string.IsNullOrWhiteSpace(m.Slug))
                    .Select(m => new { m.Slug, m.UpdatedAt, m.CreatedAt })
                    .ToListAsync(ct);

                foreach (var m in motorcycles)
                    entries.Add(new SitemapEntry(
                        BuildLoc(baseUrl, "/xe/" + Uri.EscapeDataString(m.Slug)),
                        m.UpdatedAt ?? m.CreatedAt));

                // 3. Service items — reuse ServiceCatalogService.QueryActive() visibility rule
                //    (IsActive && !IsDeleted && Category.IsActive && !Category.IsDeleted).
                var services = await db.ServiceItems.AsNoTracking()
                    .Include(s => s.Category)
                    .Where(s => s.IsActive && !s.IsDeleted
                                && s.Category.IsActive && !s.Category.IsDeleted
                                && !string.IsNullOrWhiteSpace(s.Slug))
                    .Select(s => new { s.Slug, s.UpdatedAt, s.CreatedAt })
                    .ToListAsync(ct);

                foreach (var s in services)
                    entries.Add(new SitemapEntry(
                        BuildLoc(baseUrl, "/dich-vu/" + Uri.EscapeDataString(s.Slug)),
                        s.UpdatedAt ?? s.CreatedAt));

                // 4. Blog posts — reuse BlogRepository.PublishedQuery visibility rule
                //    (IsPublished && PublishedAt <= UtcNow).
                var posts = await db.BlogPosts.AsNoTracking()
                    .Where(p => p.IsPublished
                                && p.PublishedAt != null
                                && p.PublishedAt <= DateTime.UtcNow
                                && !string.IsNullOrWhiteSpace(p.Slug))
                    .Select(p => new { p.Slug, p.PublishedAt })
                    .ToListAsync(ct);

                foreach (var p in posts)
                    entries.Add(new SitemapEntry(
                        BuildLoc(baseUrl, "/tin-tuc/" + Uri.EscapeDataString(p.Slug)),
                        p.PublishedAt));

                // 5. Promotions — reuse PromotionRepository.GetActiveAsync visibility rule
                //    (IsActive && StartDate <= now && EndDate >= now).
                //    Skip <lastmod>: StartDate/EndDate are validity windows, not modification dates.
                var now = DateTime.UtcNow;
                var promos = await db.Promotions.AsNoTracking()
                    .Where(p => p.IsActive
                                && p.StartDate <= now
                                && p.EndDate >= now
                                && !string.IsNullOrWhiteSpace(p.Slug))
                    .Select(p => p.Slug)
                    .ToListAsync(ct);

                foreach (var slug in promos)
                    entries.Add(new SitemapEntry(
                        BuildLoc(baseUrl, "/khuyen-mai/" + Uri.EscapeDataString(slug)),
                        null));

                // Deduplicate by absolute URL (case-insensitive). Preserve first occurrence.
                var deduped = new List<SitemapEntry>(entries.Count);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entries)
                {
                    if (seen.Add(entry.Url))
                        deduped.Add(entry);
                }

                var xml = BuildXml(deduped);
                return Results.Content(xml, "application/xml; charset=utf-8");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sitemap generation failed.");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        });

        app.MapGet("/robots.txt", (IOptions<SiteOptions> siteOptions) =>
        {
            var baseUrl = ResolveBaseUrl(siteOptions.Value.BaseUrl);
            var body =
                "User-agent: *\n" +
                "Allow: /\n" +
                "Disallow: /admin/\n" +
                "Disallow: /admin/api/\n" +
                $"Sitemap: {baseUrl}/sitemap.xml\n";
            return Results.Text(body, "text/plain; charset=utf-8");
        });

        return app;
    }

    private static string ResolveBaseUrl(string? configured)
    {
        var trimmed = (configured ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrEmpty(trimmed)
            || trimmed.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("onrender.com", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("hondahieunga.vn", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = DefaultBaseUrl;
        }
        return trimmed;
    }

    private static string BuildLoc(string baseUrl, string path)
    {
        var trimmedBase = baseUrl.TrimEnd('/');
        var segments = path.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);
        return $"{trimmedBase}/{string.Join('/', segments)}";
    }

    private static string BuildXml(IEnumerable<SitemapEntry> entries)
    {
        XNamespace ns = SitemapNamespace;
        var root = new XElement(ns + "urlset");

        foreach (var entry in entries)
        {
            var url = new XElement(ns + "url");
            url.Add(new XElement(ns + "loc", entry.Url));
            if (entry.LastModified.HasValue)
            {
                url.Add(new XElement(ns + "lastmod",
                    entry.LastModified.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")));
            }
            root.Add(url);
        }

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + doc.ToString(SaveOptions.DisableFormatting);
    }

    private sealed record SitemapEntry(string Url, DateTime? LastModified);
}
