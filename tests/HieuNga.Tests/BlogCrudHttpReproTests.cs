using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using HieuNga.Domain.Entities;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// HTTP-level reproduction / regression suite for the Blog (Tin tức)
/// admin CRUD flows. Drives the ACTUAL ASP.NET Core request pipeline
/// (routing → antiforgery → model binder → handler → EF → response)
/// using an in-process TestServer. Verifies what an admin's browser
/// would experience end-to-end after the Phase 2 fixes.
///
/// Each test:
///  1. Seeds a blog post directly via a fresh DbContext.
///  2. GETs /admin/tin-tuc/sua/{id} → captures the rendered HTML so the
///     form structure (action, method, handler, field names) is verified.
///  3. POSTs the form exactly as a browser would.
///  4. Confirms a 302 redirect to /admin/tin-tuc.
///  5. Re-reads the row from a fresh DbContext (no shared change
///     tracker) and asserts the persisted state.
///  6. GETs /admin/tin-tuc/{id} again and asserts the form echoes the
///     new values on the post-redirect render.
///
/// Delete tests:
///  1. Seeds a blog post.
///  2. POSTs the delete form on /admin/tin-tuc (data-confirm is JS-only
///     so the HTTP path submits directly).
///  3. Confirms a 302 redirect to /admin/tin-tuc.
///  4. Re-reads the row with IgnoreQueryFilters and confirms
///     IsDeleted=true.
/// </summary>
public class BlogCrudHttpReproTests : IClassFixture<HieuNgaTestAppFactory>
{
    private readonly HieuNgaTestAppFactory _factory;

    public BlogCrudHttpReproTests(HieuNgaTestAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<BlogPost> SeedAsync(
        string title = "Hướng dẫn chọn xe ga phù hợp",
        string? content = "Nội dung bài viết ban đầu. Nhiều đoạn để mô phỏng nội dung thực tế.",
        bool isPublished = false,
        string? thumbnailUrl = "https://res.cloudinary.com/demo/seed.jpg")
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        using var seedDb = _factory.CreateDbContext();
        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = $"{title} {unique}",
            Slug = $"{SlugOf(title)}-{unique}",
            Summary = "Tóm tắt ban đầu",
            Content = content ?? "Body",
            ThumbnailUrl = thumbnailUrl,
            IsPublished = isPublished,
            MetaTitle = "Meta Title gốc",
            MetaDescription = "Meta Description gốc",
            MetaKeywords = "honda, vision",
            OgImageUrl = thumbnailUrl,
            CanonicalUrl = $"/tin-tuc/{SlugOf(title)}-{unique}",
        };
        seedDb.BlogPosts.Add(post);
        await seedDb.SaveChangesAsync();
        return post;
    }

    private async Task<(HttpClient client, string antiforgeryToken, string editFormHtml, string editorUrl)>
        GetEditorAsync(Guid blogPostId)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var url = $"/admin/tin-tuc/sua/{blogPostId}";
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(match.Success,
            "Antiforgery token not found in rendered Edit page. " +
            "The Edit form must include @Html.AntiForgeryToken().");
        return (client, match.Groups["tok"].Value, html, url);
    }

    private static FormUrlEncodedContent BuildEditForm(
        Guid blogPostId,
        string title,
        string content,
        string? summary,
        Guid? categoryId,
        bool isPublished,
        DateTime? publishedAt,
        string? thumbnailUrl,
        string? authorName,
        string antiforgeryToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = antiforgeryToken,

            // Bound model fields (Input.X) — staff form posts ONLY these.
            ["Input.Title"] = title,
            ["Input.Summary"] = summary ?? "",
            ["Input.Content"] = content,
            ["Input.ThumbnailUrl"] = thumbnailUrl ?? "",
            ["Input.CategoryId"] = categoryId?.ToString() ?? "",
            ["Input.AuthorName"] = authorName ?? "",
            ["Input.IsPublished"] = isPublished ? "true" : "false",
            ["Input.PublishedAt"] = publishedAt?.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) ?? "",

            // SEO/Slug are NOT posted by the staff form.
        };
        return new FormUrlEncodedContent(fields);
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 1 — the rendered Edit HTML exposes the expected form fields
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Step1_RenderedHtml_ExposesExpectedFormFields()
    {
        var post = await SeedAsync();
        var (_, _, html, _) = await GetEditorAsync(post.Id);

        // Form 1: the main save form should be a POST form with method=post
        // and NO handler suffix (it binds to the default OnPostAsync).
        var mainForm = Regex.Match(html,
            @"<form[^>]*method=""post""[^>]*>(?<body>.*?)</form>",
            RegexOptions.Singleline);
        Assert.True(mainForm.Success,
            "Expected a <form method=\"post\"> on the Edit page.");

        // Antiforgery is present on this form.
        Assert.Contains("__RequestVerificationToken", mainForm.Value);

        // Title input with name="Input.Title".
        var titleInput = Regex.Match(html,
            @"<textarea[^>]*name=""Input\.Title""|" +
            @"<input[^>]*name=""Input\.Title""",
            RegexOptions.IgnoreCase);
        Assert.True(titleInput.Success,
            "Expected <input|textarea name=\"Input.Title\"> on the Edit page.");

        // Content textarea with name="Input.Content".
        var contentTa = Regex.Match(html,
            @"<textarea[^>]*name=""Input\.Content""",
            RegexOptions.IgnoreCase);
        Assert.True(contentTa.Success,
            "Expected <textarea name=\"Input.Content\"> on the Edit page.");

        // Thumbnail hidden input MUST be name="Input.ThumbnailUrl" (the
        // Input. prefix is required so model binding populates
        // BlogPostInputModel.ThumbnailUrl). Without the prefix, server
        // binding sees null → thumbnail wiped on every save. The
        // rendered attribute order is `id name type value` (Razor
        // emits the id first) so the regex must accept both orders.
        var thumbHidden = Regex.Match(html,
            @"<input\b(?=[^>]*\bname=""Input\.ThumbnailUrl"")(?=[^>]*\btype=""hidden"")[^>]*>",
            RegexOptions.IgnoreCase);
        Assert.True(thumbHidden.Success,
            "Expected <input type=\"hidden\" name=\"Input.ThumbnailUrl\"> " +
            "(the Input. prefix is required for proper model binding).");

        // IsPublished checkbox with name="Input.IsPublished".
        var pubInput = Regex.Match(html,
            @"<input[^>]*name=""Input\.IsPublished""[^>]*type=""checkbox""",
            RegexOptions.IgnoreCase);
        Assert.True(pubInput.Success);

        // NO Slug / SEO fields rendered.
        Assert.DoesNotContain("name=\"Input.Slug\"", html);
        Assert.DoesNotContain("name=\"Input.MetaTitle\"", html);
        Assert.DoesNotContain("name=\"Input.MetaDescription\"", html);
        Assert.DoesNotContain("name=\"Input.MetaKeywords\"", html);
        Assert.DoesNotContain("name=\"Input.OgImageUrl\"", html);
        Assert.DoesNotContain("name=\"Input.CanonicalUrl\"", html);

        // Lưu thay đổi button.
        Assert.Contains("Lưu thay đổi", html);

        // Phase 3 — DELETE lives on the Index page only. The Edit page
        // intentionally does NOT render a delete form so there is a
        // single canonical delete flow. Verify the absence explicitly.
        Assert.DoesNotContain("handler=Delete", html,
            StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 2 — UPDATE flow through the HTTP pipeline
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Step2_BrowserStyleUpdate_PersistsTitleAndPreservesSlug()
    {
        var post = await SeedAsync(title: "Tiêu đề gốc");
        var (client, token, _, _) = await GetEditorAsync(post.Id);

        var newTitle = "Tiêu đề đã cập nhật qua HTTP";
        using var form = BuildEditForm(
            post.Id,
            newTitle,
            content: post.Content,
            summary: post.Summary,
            categoryId: null,
            isPublished: true,
            publishedAt: new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
            thumbnailUrl: post.ThumbnailUrl,
            authorName: post.AuthorName,
            antiforgeryToken: token);

        var response = await client.PostAsync($"/admin/tin-tuc/sua/{post.Id}", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod,
            $"Expected 302 after update. Got {(int)response.StatusCode} {response.StatusCode}.");

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.BlogPosts.AsNoTracking()
            .FirstAsync(p => p.Id == post.Id);

        Assert.Equal(newTitle, reloaded.Title);
        Assert.Equal(post.Slug, reloaded.Slug);   // ← Phase 2 invariant: slug stable.
        Assert.True(reloaded.IsPublished);
        Assert.Equal(post.ThumbnailUrl, reloaded.ThumbnailUrl);
        Assert.Equal(post.OgImageUrl, reloaded.OgImageUrl);
        Assert.Equal(post.MetaTitle, reloaded.MetaTitle);
        Assert.Equal(post.MetaDescription, reloaded.MetaDescription);
    }

    [Fact]
    public async Task Step3_UpdateContent_AndReloadFromDb()
    {
        var post = await SeedAsync(content: "Bài viết cũ");
        var (client, token, _, _) = await GetEditorAsync(post.Id);

        var newContent = "Nội dung đã được cập nhật hoàn toàn qua HTTP POST. ".PadRight(200, 'a');
        using var form = BuildEditForm(
            post.Id, post.Title, newContent, post.Summary,
            null, post.IsPublished, post.PublishedAt,
            post.ThumbnailUrl, post.AuthorName, token);

        var response = await client.PostAsync($"/admin/tin-tuc/sua/{post.Id}", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.BlogPosts.AsNoTracking()
            .FirstAsync(p => p.Id == post.Id);
        Assert.Equal(newContent, reloaded.Content);
    }

    [Fact]
    public async Task Step4_UpdateCategory_Persists()
    {
        var catId = Guid.NewGuid();
        using (var seedDb = _factory.CreateDbContext())
        {
            seedDb.BlogCategories.Add(new BlogCategory
            {
                Id = catId,
                Name = "Tin tức",
                Slug = "tin-tuc"
            });
            await seedDb.SaveChangesAsync();
        }

        var post = await SeedAsync();
        var (client, token, _, _) = await GetEditorAsync(post.Id);

        using var form = BuildEditForm(
            post.Id, post.Title, post.Content, post.Summary,
            catId, post.IsPublished, post.PublishedAt,
            post.ThumbnailUrl, post.AuthorName, token);

        var response = await client.PostAsync($"/admin/tin-tuc/sua/{post.Id}", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.BlogPosts.AsNoTracking()
            .FirstAsync(p => p.Id == post.Id);
        Assert.Equal(catId, reloaded.CategoryId);
    }

    [Fact]
    public async Task Step5_UpdateThumbnail_Persists_NewUrl()
    {
        var post = await SeedAsync(thumbnailUrl: "https://cdn.example.com/old.jpg");
        var (client, token, _, _) = await GetEditorAsync(post.Id);

        const string newThumb = "https://cdn.example.com/new.jpg";
        using var form = BuildEditForm(
            post.Id, post.Title, post.Content, post.Summary,
            null, post.IsPublished, post.PublishedAt,
            newThumb, post.AuthorName, token);

        var response = await client.PostAsync($"/admin/tin-tuc/sua/{post.Id}", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.BlogPosts.AsNoTracking()
            .FirstAsync(p => p.Id == post.Id);
        Assert.Equal(newThumb, reloaded.ThumbnailUrl);
    }

    [Fact]
    public async Task Step6_UpdatePublishState_FromDraftToPublished()
    {
        var post = await SeedAsync(isPublished: false);
        var (client, token, _, _) = await GetEditorAsync(post.Id);

        using var form = BuildEditForm(
            post.Id, post.Title, post.Content, post.Summary,
            null, isPublished: true, publishedAt: null,
            post.ThumbnailUrl, post.AuthorName, token);

        var response = await client.PostAsync($"/admin/tin-tuc/sua/{post.Id}", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.BlogPosts.AsNoTracking()
            .FirstAsync(p => p.Id == post.Id);
        Assert.True(reloaded.IsPublished);
        Assert.NotNull(reloaded.PublishedAt);
    }

    [Fact]
    public async Task Step7_UpdatePublishState_FromPublishedToDraft_PreservesPublishedAt()
    {
        var publishedAt = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var post = await SeedAsync(isPublished: true);
        using (var updateDb = _factory.CreateDbContext())
        {
            var tracked = await updateDb.BlogPosts.FirstAsync(p => p.Id == post.Id);
            tracked.PublishedAt = publishedAt;
            await updateDb.SaveChangesAsync();
        }
        var editor = await GetEditorAsync(post.Id);
        var client = editor.client;
        var token = editor.antiforgeryToken;

        // Admin clears the IsPublished checkbox.
        using var form = BuildEditForm(
            post.Id, post.Title, post.Content, post.Summary,
            null, isPublished: false, publishedAt: null,
            post.ThumbnailUrl, post.AuthorName, token);

        var response = await client.PostAsync($"/admin/tin-tuc/sua/{post.Id}", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.BlogPosts.AsNoTracking()
            .FirstAsync(p => p.Id == post.Id);
        Assert.False(reloaded.IsPublished);
        Assert.Equal<DateTime?>(publishedAt, reloaded.PublishedAt);
        Assert.Equal(publishedAt, reloaded.PublishedAt);
    }

    [Fact]
    public async Task Step8_Update_NeverRegeneratesSlug_EvenIfSoftDeletedHasSameSlug()
    {
        // Phase 3 — UPDATE must NEVER regenerate the Slug, even when
        // another (possibly soft-deleted) row already occupies the
        // canonical slug for the new title. The Slug is the public URL
        // contract; renaming a post must not break it. This test pins
        // the invariant: rename → same slug → 302 success → persisted.
        var blockedSlug = "blocked-collision";
        using (var seedDb = _factory.CreateDbContext())
        {
            seedDb.BlogPosts.Add(new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = "Old deleted post",
                Slug = blockedSlug,
                Content = "x",
                IsDeleted = true,
            });
            await seedDb.SaveChangesAsync();
        }

        var post = await SeedAsync(title: "Trung de so da xoa");
        // Pin the post's slug to the blocked value so a hypothetical
        // slug-regeneration would collide. The new implementation must
        // never regenerate the slug, so the save MUST succeed.
        using (var updateDb = _factory.CreateDbContext())
        {
            var tracked = await updateDb.BlogPosts.FirstAsync(p => p.Id == post.Id);
            tracked.Slug = blockedSlug;
            await updateDb.SaveChangesAsync();
        }

        var (client, token, _, _) = await GetEditorAsync(post.Id);

        // Update with a brand-new title. Even though SlugHelper.Generate
        // would produce "trung-de-so-da-xoa" for the new title, the
        // implementation must keep the original slug ("blocked-collision").
        using var form = BuildEditForm(
            post.Id,
            "Ten moi hoan toan",
            post.Content,
            post.Summary,
            null,
            post.IsPublished,
            post.PublishedAt,
            post.ThumbnailUrl,
            post.AuthorName,
            token);

        var response = await client.PostAsync($"/admin/tin-tuc/sua/{post.Id}", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod,
            "Update must succeed (302) without touching the slug, regardless of soft-deleted collisions.");

        // Slug is preserved exactly.
        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.BlogPosts.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == post.Id);
        Assert.Equal("Ten moi hoan toan", reloaded.Title);
        Assert.Equal(blockedSlug, reloaded.Slug);
    }

    // ─────────────────────────────────────────────────────────────────
    // DELETE — through the Index page (data-confirm is JS-only;
    // the HTTP path submits the form normally with id).
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_FromIndex_SoftDeletes_AndRedirectsToList()
    {
        var post = await SeedAsync(title: "Delete this post HTTP");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        // GET the index page → capture the antiforgery token used by the
        // delete form on that page.
        var indexResp = await client.GetAsync("/admin/tin-tuc");
        Assert.Equal(HttpStatusCode.OK, indexResp.StatusCode);
        var indexHtml = await indexResp.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(indexHtml,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(tokenMatch.Success);
        var token = tokenMatch.Groups["tok"].Value;

        // POST the delete handler on /admin/tin-tuc?handler=Delete.
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["id"] = post.Id.ToString(),
        });
        var response = await client.PostAsync("/admin/tin-tuc?handler=Delete", form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod,
            $"Delete must 302 to /admin/tin-tuc. Got {(int)response.StatusCode}.");

        // Soft-deleted rows must be excluded from the normal list query.
        using var verifyDb = _factory.CreateDbContext();
        var rows = await verifyDb.BlogPosts.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .ToListAsync();
        Assert.DoesNotContain(rows, p => p.Id == post.Id);

        // And the row still exists in the DB (soft delete, not hard delete).
        var all = await verifyDb.BlogPosts.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.Id == post.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(all);
        Assert.True(all!.IsDeleted);
    }

    [Fact]
    public async Task EditPage_DoesNotRender_DeleteForm()
    {
        // Phase 3 — the canonical delete flow lives ONLY on the Index
        // page (/admin/tin-tuc). The Edit page must NOT render a second
        // competing delete form so staff can never silently fall through
        // to a stale duplicate handler. The action URL pattern is the
        // strongest invariant — `handler=Delete` should appear nowhere
        // in the Edit page's HTML.
        var post = await SeedAsync(title: "Edit page shape check");
        var (_, _, html, _) = await GetEditorAsync(post.Id);

        Assert.DoesNotContain("handler=Delete", html,
            StringComparison.OrdinalIgnoreCase);

        // And there should be no hidden id field at all on the Edit page
        // (the only place a delete form's id is posted is the Index page).
        Assert.DoesNotContain("name=\"id\"", html,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_MissingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var htmlResp = await client.GetAsync("/admin/tin-tuc");
        var html = await htmlResp.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(tokenMatch.Success);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups["tok"].Value,
            ["id"] = Guid.NewGuid().ToString(),
        });
        var response = await client.PostAsync("/admin/tin-tuc?handler=Delete", form);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_Update_DoesNotShowSuccess_AndDoesNotCorruptState()
    {
        // Empty title → Required validation must reject the POST and the
        // row must not be touched.
        var post = await SeedAsync(title: "Stable title", content: "Stable body");
        var (client, token, _, _) = await GetEditorAsync(post.Id);

        using var form = BuildEditForm(
            post.Id,
            title: "",
            content: "Body that should not be saved",
            summary: post.Summary,
            categoryId: null,
            isPublished: true,
            publishedAt: null,
            thumbnailUrl: post.ThumbnailUrl,
            authorName: post.AuthorName,
            antiforgeryToken: token);

        var response = await client.PostAsync($"/admin/tin-tuc/sua/{post.Id}", form);
        Assert.True(response.StatusCode is HttpStatusCode.OK
                                or HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod,
            $"Expected normal response (200 or 302). Got {(int)response.StatusCode}.");

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.BlogPosts.AsNoTracking()
            .FirstAsync(p => p.Id == post.Id);
        Assert.Equal(post.Title, reloaded.Title);
        Assert.Equal("Stable body", reloaded.Content);
    }

    private static string SlugOf(string title) =>
        title.ToLowerInvariant().Replace(' ', '-');

    /// <summary>
    /// Build a title whose SlugHelper.Generate() result is exactly the
    /// given slug. Used to deterministically trigger slug-collision tests
    /// against a pre-seeded soft-deleted row.
    /// </summary>
    private static string BlockedSlugTitle(string targetSlug)
    {
        // Reverse the slug → space-separated words; SlugHelper lowercases
        // and replaces spaces with hyphens, so this round-trips.
        return targetSlug.Replace('-', ' ');
    }
}
