using System.Net.Http;
using System.Threading.Tasks;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace HieuNga.Tests;

/// <summary>
/// Regression suite for the "duplicate header on desktop" bug.
///
/// Root cause was a visual composition issue, not a duplicate render:
/// the layout renders exactly one <c>&lt;header id="site-header"&gt;</c>
/// on every public page (verified via TestServer below). The article
/// <c>&lt;header class="news-detail-header"&gt;</c> on the blog detail
/// page stacks directly under the sticky site header with the same
/// pure-white background, so the two read as one continuous white
/// block at the top of the page. The fix in css/site.css gives the
/// article header a non-white surface tint.
///
/// These tests lock in both halves of that contract:
/// <list type="number">
///   <item>Layout composition — every public page renders exactly ONE
///   <c>&lt;header id="site-header"&gt;</c>, never two (which would mean
///   the partial is rendered twice somewhere).</item>
///   <item>Article header surface tint — the article <c>&lt;header&gt;</c>
///   on the blog detail page has the off-white surface tint
///   <c>news-detail-header</c> class so it visually delimits from the
///   sticky site header above it.</item>
/// </list>
/// </summary>
public class HeaderDiagnosticTests : IClassFixture<HieuNgaTestAppFactory>
{
    private readonly HieuNgaTestAppFactory _factory;
    private readonly ITestOutputHelper _out;

    public HeaderDiagnosticTests(HieuNgaTestAppFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _out = output;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/tin-tuc")]
    public async Task PublicListPage_RendersExactlyOneSiteHeader(string path)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });
        var resp = await client.GetAsync(path);
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();

        var siteHeaderCount = CountOccurrences(html, "id=\"site-header\"");
        _out.WriteLine($"[{path}] status={(int)resp.StatusCode} len={html.Length} site-header-id={siteHeaderCount}");

        Assert.True(siteHeaderCount == 1,
            $"Layout rendered {siteHeaderCount} site-headers on {path}; expected exactly 1.");
    }

    [Fact]
    public async Task MotorcycleDetailPage_RendersExactlyOneSiteHeader()
    {
        var slug = "header-regression-moto";
        await SeedMotorcycleAsync(slug);

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });
        var resp = await client.GetAsync($"/xe/{slug}");
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();

        var siteHeaderCount = CountOccurrences(html, "id=\"site-header\"");
        _out.WriteLine($"[/xe/{slug}] status={(int)resp.StatusCode} len={html.Length} site-header-id={siteHeaderCount}");

        Assert.True(siteHeaderCount == 1,
            $"Layout rendered {siteHeaderCount} site-headers on /xe/{slug}; expected exactly 1.");
    }

    [Fact]
    public async Task BlogDetailPage_HasExactlyOneSiteHeader_AndArticleHeaderHasSurfaceTint()
    {
        var slug = "header-regression-blog";
        await SeedBlogAsync(slug);

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });
        var resp = await client.GetAsync($"/tin-tuc/{slug}");
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();

        var siteHeaderCount = CountOccurrences(html, "id=\"site-header\"");
        var articleHeaderCount = CountOccurrences(html, "class=\"news-detail-header\"");

        _out.WriteLine($"[/tin-tuc/{slug}] status={(int)resp.StatusCode} len={html.Length} site-header-id={siteHeaderCount} article-header={articleHeaderCount}");

        // Lock in (1): the layout must not render the public header partial twice.
        Assert.True(siteHeaderCount == 1,
            $"Layout rendered {siteHeaderCount} site-headers on /tin-tuc/{slug}; expected exactly 1. " +
            "If this fails, the public _Header partial was included more than once (e.g. by the layout, a page, or HTML/inline JS duplication).");

        // Lock in (2): the article <header> must still exist (semantically correct,
        // SEO + accessibility), and must carry the news-detail-header class so the
        // surface-tint CSS actually targets it.
        Assert.True(articleHeaderCount == 1,
            $"Expected exactly one <header class=\"news-detail-header\"> on /tin-tuc/{slug}, found {articleHeaderCount}. " +
            "The article <header> is the second <header> on this page; if you remove it, accessibility/SEO regress.");

        // The article header element must NOT carry the same site-header identity /
        // styling that would render it as a duplicate navigation bar.
        Assert.DoesNotContain("id=\"site-header\" class=\"news-detail-header", html);
        Assert.DoesNotContain("class=\"site-header news-detail-header", html);
    }

    private async Task SeedBlogAsync(string slug)
    {
        using var db = _factory.CreateDbContext();
        db.BlogPosts.Add(new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết kiểm thử header",
            Slug = slug,
            Summary = "Tóm tắt",
            Content = "Nội dung kiểm thử.",
            ThumbnailUrl = "https://example.com/x.webp",
            IsPublished = true,
            PublishedAt = DateTime.UtcNow.AddMinutes(-1),
            IsDeleted = false,
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedMotorcycleAsync(string slug)
    {
        using var db = _factory.CreateDbContext();
        db.Motorcycles.Add(new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Test Bike",
            Slug = slug,
            Category = MotorcycleCategory.Scooter,
            BasePrice = 41_290_000m,
            ShortDescription = "Mô tả ngắn",
            Description = "Mô tả dài",
            IsPublished = true,
        });
        await db.SaveChangesAsync();
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
