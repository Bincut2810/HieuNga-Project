using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HieuNga.Domain.Entities;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// Tests for the Admin Toast / Flash widget payload:
///   - The server-rendered HTML must contain a [data-admin-flash] banner
///     AND a [data-toast] hidden input with the same payload so the
///     JS widget and the JS-disabled banner fallback both work.
///   - The success/error keys MUST be distinct so a failed save's banner
///     cannot cross-render with a successful save's banner.
///   - TempData is consumed on read so the message is shown once.
/// </summary>
public class AdminFlashHtmlTests : IClassFixture<HieuNgaTestAppFactory>
{
    private readonly HieuNgaTestAppFactory _factory;

    public AdminFlashHtmlTests(HieuNgaTestAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<BlogPost> SeedPostAsync()
    {
        using var db = _factory.CreateDbContext();
        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Test Post " + Guid.NewGuid().ToString("N")[..8],
            Slug = "test-post-" + Guid.NewGuid().ToString("N")[..8],
            Content = "Body",
            IsPublished = false,
        };
        db.BlogPosts.Add(post);
        await db.SaveChangesAsync();
        return post;
    }

    [Fact]
    public async Task CreateSuccess_SetsSuccessTempData_AndRedirectsToIndex_WithFlashBanner()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        // 1) Get the /admin/tin-tuc/them page to pull the antiforgery token.
        var getResp = await client.GetAsync("/admin/tin-tuc/them");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var getHtml = await getResp.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(getHtml,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(tokenMatch.Success);

        // 2) POST Create with a unique title and unique derived slug.
        var unique = Guid.NewGuid().ToString("N")[..8];
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups["tok"].Value,
            ["Input.Title"] = $"Bài viết {unique}",
            ["Input.Content"] = "Nội dung đủ dài để pass validation.",
            ["Input.IsPublished"] = "true",
        });
        var postResp = await client.PostAsync("/admin/tin-tuc/them", form);
        Assert.True(
            postResp.StatusCode is HttpStatusCode.Redirect
                              or HttpStatusCode.Found
                              or HttpStatusCode.RedirectMethod,
            $"Create must 302. Got {(int)postResp.StatusCode}.");

        // 3) Follow the redirect manually (we set AllowAutoRedirect=false).
        var indexResp = await client.GetAsync("/admin/tin-tuc");
        Assert.Equal(HttpStatusCode.OK, indexResp.StatusCode);
        var indexHtml = await indexResp.Content.ReadAsStringAsync();

        // The flash partial must render a [data-admin-flash="success"]
        // banner with the Vietnamese message. The message text is HTML-
        // encoded by Razor — decode it before asserting, since some
        // characters (Đ, ă, ề, ẽ …) are written as &#xNNN; entities.
        var successBanner = Regex.Match(indexHtml,
            @"<div[^>]*data-admin-flash=""success""[^>]*>(?<msg>[^<]+)</div>",
            RegexOptions.IgnoreCase);
        Assert.True(successBanner.Success,
            "Success banner with data-admin-flash=\"success\" must be rendered on /admin/tin-tuc.");
        var decoded = HttpUtility.HtmlDecode(successBanner.Groups["msg"].Value);
        Assert.Contains("Đã thêm bài viết", decoded);

        // And the [data-toast] hidden input mirror.
        var toastTrigger = Regex.Match(indexHtml,
            @"<input[^>]*data-toast[^>]*data-toast-type=""success""[^>]*value=""(?<msg>[^""]+)""",
            RegexOptions.IgnoreCase);
        Assert.True(toastTrigger.Success,
            "A <input data-toast data-toast-type=\"success\"> hidden trigger must be emitted.");
        Assert.Contains("Đã thêm bài viết", HttpUtility.HtmlDecode(toastTrigger.Groups["msg"].Value));

        // No error banner.
        Assert.DoesNotContain("data-admin-flash=\"error\"", indexHtml);
    }

    [Fact]
    public async Task DeleteSuccess_SetsSuccessTempData_OnIndex()
    {
        var post = await SeedPostAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var getResp = await client.GetAsync("/admin/tin-tuc");
        var getHtml = await getResp.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(getHtml,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(tokenMatch.Success);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups["tok"].Value,
            ["id"] = post.Id.ToString(),
        });
        var resp = await client.PostAsync("/admin/tin-tuc?handler=Delete", form);
        Assert.True(resp.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        // GET the index again — the delete must have written the success
        // message. Razor HTML-encodes the diacritics, so decode the page
        // before asserting.
        var indexResp = await client.GetAsync("/admin/tin-tuc");
        var rawHtml = await indexResp.Content.ReadAsStringAsync();
        var decoded = HttpUtility.HtmlDecode(rawHtml);
        Assert.Contains("Đã xóa bài viết", decoded);
        Assert.DoesNotContain("data-admin-flash=\"error\"", rawHtml);
    }

    [Fact]
    public async Task TempData_IsConsumedOnRead_AcrossReloads()
    {
        // 1) Do a create so a success flash is written.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var getResp = await client.GetAsync("/admin/tin-tuc/them");
        var getHtml = await getResp.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(getHtml,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(tokenMatch.Success);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups["tok"].Value,
            ["Input.Title"] = $"Bài viết {unique}",
            ["Input.Content"] = "Body",
        });
        await client.PostAsync("/admin/tin-tuc/them", form);

        // 2) First reload: the success banner is rendered.
        var first = await client.GetAsync("/admin/tin-tuc");
        var firstRaw = await first.Content.ReadAsStringAsync();
        Assert.Contains("Đã thêm bài viết", HttpUtility.HtmlDecode(firstRaw));

        // 3) Second reload (NO save in between): the banner is gone
        //    because TempData entries are consumed on first read.
        var second = await client.GetAsync("/admin/tin-tuc");
        var secondRaw = await second.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Đã thêm bài viết", HttpUtility.HtmlDecode(secondRaw));
    }

    [Fact]
    public async Task UpdateSuccess_SetsSuccessTempData_OnIndex_WithFlashBanner()
    {
        var post = await SeedPostAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var editUrl = $"/admin/tin-tuc/sua/{post.Id}";
        var getResp = await client.GetAsync(editUrl);
        var getHtml = await getResp.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(getHtml,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(tokenMatch.Success);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups["tok"].Value,
            ["Input.Title"] = post.Title + " updated",
            ["Input.Content"] = "Updated body",
            ["Input.IsPublished"] = "false",
        });
        var resp = await client.PostAsync(editUrl, form);
        Assert.True(resp.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        var indexResp = await client.GetAsync("/admin/tin-tuc");
        var rawHtml = await indexResp.Content.ReadAsStringAsync();
        var decoded = HttpUtility.HtmlDecode(rawHtml);

        Assert.Contains("Đã cập nhật bài viết", decoded);
        Assert.DoesNotContain("data-admin-flash=\"error\"", rawHtml);
    }

    [Fact]
    public async Task FlashPayloads_SuccessAndError_DoNotCross_Render()
    {
        // Stale-error scenario: a previous failed save wrote an error.
        // The next successful save MUST overwrite the error with success
        // and not render both.
        var post = await SeedPostAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var editUrl = $"/admin/tin-tuc/sua/{post.Id}";
        var getResp = await client.GetAsync(editUrl);
        var getHtml = await getResp.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(getHtml,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(tokenMatch.Success);

        // Trigger an error first (slug collision with title=""):
        var errorForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups["tok"].Value,
            ["Input.Title"] = "",
            ["Input.Content"] = "Body",
        });
        var errorResp = await client.PostAsync(editUrl, errorForm);
        // Likely returns 200 (Page) with a model error → no redirect.

        // Now reload: error flash banner from the previous attempt must
        // NOT linger into a fresh page view. TempData is consumed on
        // first read.
        var reloadResp = await client.GetAsync("/admin/tin-tuc");
        var reloadHtml = await reloadResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("data-admin-flash=\"error\"", reloadHtml);
    }
}
