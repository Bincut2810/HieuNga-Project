using HieuNga.Domain.Entities;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Repositories;
using HieuNga.Web.Pages.Admin;
using HieuNga.Web.Pages.Admin.TinTuc;
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
/// Phase 3 rebuild — exercises the dedicated page models in
/// <c>Pages/Admin/TinTuc/</c> and the <see cref="BlogPostInputModel"/>
/// that the strongly-typed <c>_BlogPostForm.cshtml</c> binds to.
///
/// Each test calls the actual page-model handlers through the same
/// code path the HTTP request pipeline takes (HttpContext +
/// PageContext + TempData) and re-reads the persisted row from a
/// fresh DbContext so the change tracker can't mask an un-persisted
/// change.
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

    private HieuNga.Web.Pages.Admin.TinTuc.IndexModel CreateIndexPage()
    {
        var page = new HieuNga.Web.Pages.Admin.TinTuc.IndexModel(
            _db, _repo, _uow, NullLogger<HieuNga.Web.Pages.Admin.TinTuc.IndexModel>.Instance);
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
            Slug = $"{title.ToLowerInvariant().Replace(' ', '-')}-{seed}",
            Summary = "Tóm tắt ban đầu",
            Content = content,
            ThumbnailUrl = thumbnailUrl,
            IsPublished = isPublished,
            PublishedAt = publishedAt,
            MetaTitle = $"{title} | Hiếu Nga",
            MetaDescription = "Mô tả meta gốc",
            OgImageUrl = thumbnailUrl,
        };
    }

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
        Assert.Equal("Đã thêm bài viết", page.TempData["AdminSuccess"]);

        var reloaded = await _db.BlogPosts.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .FirstAsync();
        Assert.Equal("Hướng dẫn chọn xe ga phù hợp", reloaded.Title);
        Assert.Equal("huong-dan-chon-xe-ga-phu-hop", reloaded.Slug);
        Assert.True(reloaded.IsPublished);
        Assert.NotNull(reloaded.PublishedAt);
        // Phase 4 invariant: PublishedAt must be UTC so it survives the
        // PostgreSQL `timestamp with time zone` mapping. The InMemory
        // provider accepts any Kind, so the assertion is the strongest
        // check the non-PostgreSQL test suite can make.
        Assert.Equal(DateTimeKind.Utc, reloaded.PublishedAt!.Value.Kind);
        // BaseEntity.CreatedAt is also timestamptz.
        Assert.Equal(DateTimeKind.Utc, reloaded.CreatedAt.Kind);
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
    public async Task Create_MissingContent_DoesNotPersist()
    {
        var page = CreateThemPage();
        page.Input = new BlogPostInputModel { Title = "Có tiêu đề", Content = "" };
        page.ModelState.AddModelError("Input.Content", "Vui lòng nhập nội dung");

        var result = await page.OnPostAsync(CancellationToken.None);
        Assert.IsType<PageResult>(result);
        Assert.Equal(0, await _db.BlogPosts.CountAsync());
    }

    [Fact]
    public async Task Create_DuplicateTitleAgainstSoftDeleted_DoesNotPersist()
    {
        // SlugHelper.Generate strips Vietnamese diacritics ("ù","ề","ố","ị","ặ"...)
        // and replaces "đ" with "d", so a title of "Trung de so bi chan" produces
        // slug "trung-de-so-bi-chan" exactly. Pre-seed a soft-deleted row with
        // that slug so the application's IgnoreQueryFilters() uniqueness check
        // catches the collision before SaveChangesAsync.
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
            Title = "Trung de so bi chan",
            Content = "Nội dung",
        };
        var result = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.True(page.ModelState.ContainsKey("Input.Title"),
            "Must surface a friendly error when slug collides with a soft-deleted post.");
        Assert.Equal(1, await _db.BlogPosts.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Create_SaveFailure_DoesNotSetSuccess_AndDoesNotPersist()
    {
        // Force a unique-index violation by pre-seeding a row with the
        // exact slug the new post would auto-generate. The InMemory
        // provider does NOT enforce unique indexes by default, so we
        // simulate a save failure by using a duplicate add of the
        // same key via a different mechanism: a deliberately broken
        // path that throws inside SaveChangesAsync is hard to trigger
        // against the InMemory provider. We instead use a slug that
        // collides via the application-level pre-check, which the
        // test now verifies is a validation error, not a 500.
        var existing = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Existing",
            Slug = "existing-slug",
            Content = "x",
        };
        _db.BlogPosts.Add(existing);
        await _db.SaveChangesAsync();

        var page = CreateThemPage();
        page.Input = new BlogPostInputModel
        {
            Title = "Existing Slug",
            Content = "Body",
        };
        var result = await page.OnPostAsync(CancellationToken.None);
        Assert.IsType<PageResult>(result);
        Assert.Null(page.TempData["AdminSuccess"]);
        // No second row added.
        Assert.Equal(1, await _db.BlogPosts.CountAsync());
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
        Assert.Equal(seed.Slug, reloaded.Slug);   // critical: slug must NOT regenerate
        Assert.NotNull(reloaded.PublishedAt);      // preserved
    }

    [Fact]
    public async Task Update_Summary_Persists()
    {
        var seed = SeedBlog();
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.Summary = "Tóm tắt đã cập nhật";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal("Tóm tắt đã cập nhật", reloaded.Summary);
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
    public async Task Update_Thumbnail_Persists_NewUrl()
    {
        var seed = SeedBlog(thumbnailUrl: "https://cdn.example.com/old.jpg");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.ThumbnailUrl = "https://cdn.example.com/new.jpg";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal("https://cdn.example.com/new.jpg", reloaded.ThumbnailUrl);
    }

    [Fact]
    public async Task Update_Thumbnail_EmptyInput_KeepsExistingThumbnail()
    {
        var seed = SeedBlog(thumbnailUrl: "https://cdn.example.com/keep.jpg");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        // Staff did not upload a new image — the uploader's hidden
        // input may still post an empty string. The page model must
        // keep the existing ThumbnailUrl.
        page.Input.ThumbnailUrl = "";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal("https://cdn.example.com/keep.jpg", reloaded.ThumbnailUrl);
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
    public async Task Update_Author_Persists()
    {
        var seed = SeedBlog();
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.AuthorName = "Tác giả mới";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal("Tác giả mới", reloaded.AuthorName);
    }

    [Fact]
    public async Task Update_PublishedAt_StaffOverride_NoLongerExposedViaForm()
    {
        // The Edit form no longer renders a PublishedAt input — staff
        // can't set an arbitrary date. The page-model owns the field
        // and stamps it from DateTime.UtcNow when first publishing.
        // This test pins the new contract: the page model never reads
        // a PublishedAt from the form binding (the property is absent
        // from BlogPostInputModel after the Phase 4 rebuild).
        var seed = SeedBlog(isPublished: true, publishedAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);

        // BlogPostInputModel does NOT expose PublishedAt anymore.
        var inputType = page.Input.GetType();
        Assert.Null(inputType.GetProperty("PublishedAt"));

        // Saving without IsPublished change must preserve the historical date.
        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal<DateTime?>(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            reloaded.PublishedAt);
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

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.True(reloaded.IsPublished);
        Assert.NotNull(reloaded.PublishedAt);
        // Phase 4 invariant: the new PublishedAt must be UTC so it can
        // be written to the PostgreSQL timestamptz column without an
        // exception. The InMemory provider does not enforce Kind rules,
        // so this assertion is the only thing the test suite can verify
        // without a real PostgreSQL — a real PostgreSQL test would
        // additionally observe that SaveChangesAsync did not throw
        // InvalidCastException for Kind=Unspecified.
        Assert.Equal(DateTimeKind.Utc, reloaded.PublishedAt!.Value.Kind);
    }

    [Fact]
    public async Task Update_PublishState_DraftToDraft_KeepsExistingPublishedAt()
    {
        var publishedAt = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
        var seed = SeedBlog(isPublished: false, publishedAt: publishedAt);
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
    }

    [Fact]
    public async Task Update_PublishState_PublishedToPublished_KeepsPublishedAt()
    {
        var publishedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var seed = SeedBlog(isPublished: true, publishedAt: publishedAt);
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.IsPublished = true;

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.True(reloaded.IsPublished);
        Assert.Equal<DateTime?>(publishedAt, reloaded.PublishedAt);
    }

    [Fact]
    public async Task Update_DoesNotWipeSeoFields()
    {
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
    public async Task Update_DoesNotChangeCreatedAt()
    {
        var seed = SeedBlog();
        var originalCreatedAt = seed.CreatedAt;
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateSuaPage();
        await page.OnGetAsync(seed.Id, CancellationToken.None);
        page.Input.Title = "Đã đổi";
        page.Input.Content = "Nội dung đã đổi";

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal(originalCreatedAt, reloaded.CreatedAt);
        Assert.False(reloaded.IsDeleted);
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
        page.Input.Title = "";
        page.Input.Content = "Should not be saved";
        page.ModelState.AddModelError("Input.Title", "Vui lòng nhập tiêu đề");

        var result = await page.OnPostAsync(seed.Id, CancellationToken.None);
        Assert.IsType<PageResult>(result);

        var reloaded = await _db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == seed.Id);
        Assert.Equal(seed.Title, reloaded.Title);
        Assert.Equal("Stable", reloaded.Content);
    }

    // ─────────────────────────────────────────────────────────────────
    // DELETE — flows through the Index page's OnPostDeleteAsync.
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_OnIndex_SoftDeletesRow_AndSetsIsDeletedTrue()
    {
        var seed = SeedBlog(title: "Delete Me");
        _db.BlogPosts.Add(seed);
        await _db.SaveChangesAsync();

        var page = CreateIndexPage();
        var result = await page.OnPostDeleteAsync(seed.Id, CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Đã xóa bài viết.", page.TempData["AdminSuccess"]);

        var reloaded = await _db.BlogPosts.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == seed.Id);
        Assert.True(reloaded.IsDeleted);
        Assert.NotNull(reloaded.UpdatedAt);
        // Phase 4 invariant: UpdatedAt is set by the repository to
        // DateTime.UtcNow, which is valid for the timestamptz column.
        Assert.Equal(DateTimeKind.Utc, reloaded.UpdatedAt!.Value.Kind);
    }

    [Fact]
    public async Task Delete_OnIndex_ExcludesFromListQuery()
    {
        var keep = SeedBlog(title: "Keep");
        var drop = SeedBlog(title: "Drop");
        _db.BlogPosts.AddRange(keep, drop);
        await _db.SaveChangesAsync();

        var page = CreateIndexPage();
        await page.OnPostDeleteAsync(drop.Id, CancellationToken.None);

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

        var page = CreateIndexPage();
        var result = await page.OnPostDeleteAsync(seed.Id, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_MissingId_ReturnsNotFound()
    {
        var page = CreateIndexPage();
        var result = await page.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_DoesNotShowSuccess_OnFailure()
    {
        var page = CreateIndexPage();
        var result = await page.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
        Assert.Null(page.TempData["AdminSuccess"]);
    }

    // ─────────────────────────────────────────────────────────────────
    // Helper — minimal ITempDataProvider so the handler can write/read
    // TempData during tests.
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
