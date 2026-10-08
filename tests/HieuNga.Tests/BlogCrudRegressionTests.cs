using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Repositories;
using HieuNga.Web.Pages.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// End-to-end regression tests for the Blog (Tin tức) admin CRUD.
///
/// Each test calls the actual PageModel handlers through the same code
/// path the HTTP request pipeline takes (HttpContext + PageContext +
/// TempData). Asserts verify the persistence outcome in the database
/// from a fresh DbContext so no change tracker or in-memory cache can
/// mask an un-persisted change.
///
/// Phase 2 — covers the production bugs:
///   1. Blog UPDATE silently wipes SEO/Slug/Thumbnail (it did).
///   2. Slug regeneration on every edit breaks the public URL.
///   3. Unpublishing a post nullifies its historical PublishedAt.
///   4. Slug-collision check excludes soft-deleted rows → unique
///      constraint violation 23505 fires inside SaveChangesAsync → 500.
///   5. Blog DELETE flow correctly soft-deletes + redirects + ToastData.
///   6. The Image uploader partial MUST be invoked with the bound
///      property's qualified name (Input.ThumbnailUrl), not the bare
///      property name — otherwise model binding never sees the URL.
/// </summary>
public class BlogCrudRegressionTests : IDisposable
{
    private readonly HieuNgaDbContext _db;
    private readonly IRepository<BlogPost> _repo;
    private readonly IUnitOfWork _uow;

    public BlogCrudRegressionTests()
    {
        var opts = new DbContextOptionsBuilder<HieuNgaDbContext>()
            .UseInMemoryDatabase($"BlogCrudRegressionTests-{Guid.NewGuid():N}")
            .Options;
        _db = new HieuNgaDbContext(opts);
        _repo = new Repository<BlogPost>(_db);
        _uow = new UnitOfWork(_db);
    }

    public void Dispose() => _db.Dispose();

    private TinTucThemModel CreateThemPage()
    {
        var page = new TinTucThemModel(_repo, _uow, _db, NullLogger<TinTucThemModel>.Instance);
        AttachPage(page);
        return page;
    }

    private TinTucSuaModel CreateSuaPage()
    {
        var page = new TinTucSuaModel(_repo, _uow, _db, NullLogger<TinTucSuaModel>.Instance);
        AttachPage(page);
        return page;
    }

    private static void AttachPage(PageModel page)
    {
        var http = new DefaultHttpContext { TraceIdentifier = "test-trace" };
        var actionContext = new ActionContext(http, new RouteData(), new PageActionDescriptor(), page.ModelState);
        var pageContext = new PageContext(actionContext)
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), page.ModelState)
        };
        page.PageContext = pageContext;
        page.TempData = new TempDataDictionary(http, new RecordingTempDataProvider());
    }

    private static BlogPost SeedBlog(
        string title = "Bài viết ban đầu",
        string content = "Nội dung ban đầu",
        bool isPublished = false,
        DateTime? publishedAt = null,
        string? thumbnailUrl = "https://res.cloudinary.com/demo/seed.jpg")
    {
        var seed = Guid.NewGuid().ToString("N")[..8];
        return new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = $"{title} {seed}",
            Slug = $"{SlugOf(title)}-{seed}",
            Summary = "Tóm tắt ban đầu",
            Content = content,
            ThumbnailUrl = thumbnailUrl,
            IsPublished = isPublished,
            PublishedAt = publishedAt,
            // Phase 2 - auto-seeded on create.
            MetaTitle = $"{title} | Hiếu Nga",
            MetaDescription = "Mô tả meta gốc",
            OgImageUrl = thumbnailUrl,
        };
    }

    private static string SlugOf(string title) =>
        title.ToLowerInvariant().Replace(' ', '-');

    // ─────────────────────────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Persists_AndAutogeneratesSlugAndSeo()
    {
        var page = CreateThemPage();
        page.Input = new BlogPostInputModel
        {
            Title = "Hướng dẫn chọn xe ga phù hợp",
            Content = "Nội dung dài của bài viết.",
            Summary = "Tóm tắt ngắn gọn",
            IsPublished = true,
            ThumbnailUrl = "https://cdn.example.com/cover.jpg",
        };

        var result = await page.OnPostAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Đã thêm bài viết.", page.TempData["AdminSuccess"]);

        var reloaded = await _db.BlogPosts.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .FirstAsync();
        Assert.Equal("Hướng dẫn chọn xe ga phù hợp", reloaded.Title);
        Assert.Equal("huong-dan-chon-xe-ga-phu-hop", reloaded.Slug);
        Assert.True(reloaded.IsPublished);
        Assert.NotNull(reloaded.PublishedAt);
        Assert.Equal("https://cdn.example.com/cover.jpg", reloaded.ThumbnailUrl);
        Assert.Equal("https://cdn.example.com/cover.jpg", reloaded.OgImageUrl);
        Assert.Equal("Hướng dẫn chọn xe ga phù hợp | Hiếu Nga", reloaded.MetaTitle);
        Assert.Equal("Tóm tắt ngắn gọn", reloaded.MetaDescription);
    }

    [Fact]
    public async Task Create_Draft_PublishedAtIsNull()
    {
        var page = CreateThemPage();
        page.Input = new BlogPostInputModel
        {
            Title = "Bản nháp thuần",
            Content = "Nội dung dài.",
            IsPublished = false,
        };

        var result = await page.OnPostAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .FirstAsync();
        Assert.False(reloaded.IsPublished);
        Assert.Null(reloaded.PublishedAt);
    }

    [Fact]
    public async Task Create_MissingTitle_DoesNotPersist()
    {
        var page = CreateThemPage();
        page.Input = new BlogPostInputModel { Title = "", Content = "Body" };
        page.ModelState.AddModelError("Input.Title", "Vui lòng nhập tiêu đề");

        var result = await page.OnPostAsync(CancellationToken.None);
        Assert.IsType<PageResult>(result);
        Assert.Equal(0, await _db.BlogPosts.CountAsync());
    }

    [Fact]
    public async Task Create_DuplicateTitleAgainstSoftDeleted_DoesNotPersist()
    {
        // Existing soft-deleted post uses a SPECIFIC slug we control.
        // A new create with a title that slugifies to the same value
        // must fail friendly, NOT crash with a DB-level 23505 unique
        // violation. The seeded post's title is set to a non-conflicting
        // value to keep the only collision driver the auto-generated slug.
        var blockedSlug = "trung-de-so-bi-chan";
        var seed = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Bất kỳ — chỉ để có hàng",
            Slug = blockedSlug,
            Content = "x",
            IsDeleted = true,
        };
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateThemPage();
        page.Input = new BlogPostInputModel
        {
            // Title whose slug derives to exactly the blocked slug.
            Title = "Trùng đề số bị chặn",
            Content = "Nội dung",
        };
        var result = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.True(page.ModelState.ContainsKey("Input.Title"),
            "Must surface a friendly error when slug collides with a soft-deleted post.");
        // The seed must still exist (soft-deleted) and the new create
        // must NOT have been added. Use IgnoreQueryFilters so the
        // global soft-delete filter doesn't hide the seed.
        Assert.Equal(1, await _db.BlogPosts.IgnoreQueryFilters().CountAsync());
    }

    // ─────────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Title_Persists_AndPreservesSlug()
    {
        var seed = SeedBlog(title: "Bài viết gốc", isPublished: true,
            publishedAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.Title = "Bài viết đã đổi tên";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal("Bài viết đã đổi tên", reloaded.Title);
        Assert.Equal(seed.Slug, reloaded.Slug);   // ← critical: slug must NOT regenerate
        Assert.NotNull(reloaded.PublishedAt);      // preserved
    }

    [Fact]
    public async Task Update_Content_Persists()
    {
        var seed = SeedBlog(content: "Nội dung cũ");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.Content = "Nội dung mới dài hơn nhiều hơn nhiều hơn.";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal("Nội dung mới dài hơn nhiều hơn nhiều hơn.", reloaded.Content);
    }

    [Fact]
    public async Task Update_Category_Persists()
    {
        var category = new BlogCategory { Id = Guid.NewGuid(), Name = "Tin tức" };
        _db.BlogCategories.Add(category);
        await _db.SaveChangesAsync();

        var seed = SeedBlog();
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.CategoryId = category.Id;

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal(category.Id, reloaded.CategoryId);
    }

    [Fact]
    public async Task Update_Thumbnail_Persists_AndDoesNotWipeOtherFields()
    {
        var seed = SeedBlog(thumbnailUrl: "https://cdn.example.com/old.jpg");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.ThumbnailUrl = "https://cdn.example.com/new.jpg";
        page.Input.Title = "Tiêu đề tạm thời";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal("https://cdn.example.com/new.jpg", reloaded.ThumbnailUrl);
        Assert.Equal("Tiêu đề tạm thời", reloaded.Title);
    }

    [Fact]
    public async Task Update_PublishState_Draft_PreservesPreviousPublishedAt()
    {
        var publishedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var seed = SeedBlog(isPublished: true, publishedAt: publishedAt);
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.IsPublished = false;

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.False(reloaded.IsPublished);
        Assert.Equal<DateTime?>(publishedAt, reloaded.PublishedAt);
        Assert.Equal(publishedAt, reloaded.PublishedAt);
    }

    [Fact]
    public async Task Update_PublishState_Published_SetsPublishedAtIfEmpty()
    {
        var seed = SeedBlog(isPublished: false, publishedAt: null);
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.IsPublished = true;
        page.Input.PublishedAt = null;

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.True(reloaded.IsPublished);
        Assert.NotNull(reloaded.PublishedAt);
    }

    [Fact]
    public async Task Update_DoesNotWipeSeoFields()
    {
        // Phase 2 — SEO fields are server-managed, not posted by staff.
        // The update must preserve whatever SEO values were set previously.
        var seed = SeedBlog(title: "Seo Title Test");
        seed.MetaTitle = "Meta Title gốc";
        seed.MetaDescription = "Meta Description gốc";
        seed.MetaKeywords = "honda, vision";
        seed.OgImageUrl = "https://cdn.example.com/og.jpg";
        seed.CanonicalUrl = "/tin-tuc/seo-title-test";
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.Title = "Seo Title Test Updated";
        page.Input.Content = "Nội dung đã cập nhật";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal("Seo Title Test Updated", reloaded.Title);
        Assert.Equal("Meta Title gốc", reloaded.MetaTitle);
        Assert.Equal("Meta Description gốc", reloaded.MetaDescription);
        Assert.Equal("honda, vision", reloaded.MetaKeywords);
        Assert.Equal("https://cdn.example.com/og.jpg", reloaded.OgImageUrl);
        Assert.Equal("/tin-tuc/seo-title-test", reloaded.CanonicalUrl);
    }

    [Fact]
    public async Task Update_DoesNotCreateDuplicate()
    {
        var seed = SeedBlog();
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.Title = "Đã đổi";
        page.Input.Content = "Nội dung đã đổi";

        await page.OnPostAsync(seed.Id, CancellationToken.None);

        Assert.Single(await _db.BlogPosts.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        var page = CreateSuaPage();
        page.Input = new BlogPostInputModel
        {
            Title = "Bất kỳ",
            Content = "Body"
        };
        var result = await page.OnPostAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_SoftDeleted_ReturnsNotFound()
    {
        var seed = SeedBlog();
        seed.IsDeleted = true;
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        page.Input = new BlogPostInputModel { Title = "Bất kỳ", Content = "Body" };
        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_GetAfterPost_ShowsPersistedValues()
    {
        var seed = SeedBlog(title: "Original Title", content: "Original Body");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.Title = "Updated Title";
        page.Input.Content = "Updated Body";
        await page.OnPostAsync(seed.Id, CancellationToken.None);

        var page2 = CreateSuaPage();
        await page2.OnGetAsync(seed.Id, CancellationToken.None);

        Assert.Equal("Updated Title", page2.Input.Title);
        Assert.Equal("Updated Body", page2.Input.Content);
    }

    [Fact]
    public async Task Update_NoChanges_Succeeds_AndPersistsUntouched()
    {
        var seed = SeedBlog(title: "Stable", content: "Stable");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal(seed.Title, reloaded.Title);
        Assert.Equal(seed.Content, reloaded.Content);
        Assert.Equal(seed.Slug, reloaded.Slug);
        Assert.Equal(seed.ThumbnailUrl, reloaded.ThumbnailUrl);
    }

    [Fact]
    public async Task Update_InvalidTitle_DoesNotPersistOtherFields()
    {
        var seed = SeedBlog(title: "Should remain", content: "Stable");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.Title = ""; // clear the title but keep the rest
        page.Input.Content = "Should not be saved";
        page.ModelState.AddModelError("Input.Title", "Vui lòng nhập tiêu đề");

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<PageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal(seed.Title, reloaded.Title);
        Assert.Equal("Stable", reloaded.Content);
    }

    // ─────────────────────────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletesRow_AndSetsIsDeletedTrue()
    {
        var seed = SeedBlog(title: "Delete Me");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        var result = await page.OnPostDeleteAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Đã xóa bài viết.", page.TempData["AdminSuccess"]);

        var reloaded = await _db.BlogPosts.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == seed.Id);
        Assert.True(reloaded.IsDeleted);
        Assert.NotNull(reloaded.UpdatedAt);
    }

    [Fact]
    public async Task Delete_OnIndex_ExcludesFromListQuery()
    {
        var keep = SeedBlog(title: "Keep");
        var drop = SeedBlog(title: "Drop");
        _db.BlogPosts.AddRange(keep, drop);
        await _db.SaveChangesAsync();

        // Soft-delete drop via the page model.
        var page = CreateSuaPage();
        await page.OnPostDeleteAsync(drop.Id, CancellationToken.None);

        // Normal list query (with the soft-delete query filter applied)
        // must NOT return the deleted row.
        var rows = await _db.BlogPosts.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .ToListAsync();
        Assert.Contains(rows, p => p.Id == keep.Id);
        Assert.DoesNotContain(rows, p => p.Id == drop.Id);
    }

    [Fact]
    public async Task Delete_IdempotentOnAlreadyDeleted_ReturnsNotFound()
    {
        var seed = SeedBlog();
        seed.IsDeleted = true;
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        var result = await page.OnPostDeleteAsync(seed.Id, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_MissingId_ReturnsNotFound()
    {
        var page = CreateSuaPage();
        var result = await page.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    // ─────────────────────────────────────────────────────────────────
    // Helper — minimal ITempDataProvider so the handler can write/read
    // TempData during tests. Returns null so the assertions can check
    // whether the handler wrote a value at all.
    // ─────────────────────────────────────────────────────────────────

    private sealed class RecordingTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) =>
            new Dictionary<string, object?>();

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
        {
            // PageModel.TempData buffers the values; no-op on save.
        }
    }
}
