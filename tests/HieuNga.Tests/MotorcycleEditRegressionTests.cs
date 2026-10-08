using HieuNga.Application.Interfaces;
using HieuNga.Application.Media;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using HieuNga.Infrastructure.Repositories;
using HieuNga.Web.Pages.Admin.Xe;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HieuNga.Tests;

/// <summary>
/// End-to-end regression tests for the Motorcycle admin EDIT flow.
///
/// Background — the production editor had two bugs that combined to look
/// like "save appears to work but the DB value never changes":
///
///   1. The General-tab &lt;select&gt; for <c>Category</c> contained an
///      empty <c>&lt;option value=""&gt;-- Chọn loại xe --&lt;/option&gt;</c>.
///      Selecting it posted <c>Input.Category=""</c>, which the default model
///      binder cannot convert to <c>MotorcycleCategory</c>. ModelState went
///      invalid but no <c>asp-validation-for</c> span rendered the error, so
///      the user saw their typed <c>BasePrice</c> / <c>Name</c> reflected
///      back at them and assumed the save had worked.
///
///   2. <c>SaveCoreAsync</c> called <c>ApplyPublishStatusToInput()</c> which
///      unconditionally flipped <c>Input.IsPublished</c> back to <c>false</c>
///      (because the General tab posts no <c>PublishStatus</c> field, so it
///      defaulted to "draft"). Every save from General un-published the bike.
///
/// Two more silent regressions were fixed alongside:
///   - <c>MetaTitle</c> / <c>MetaDescription</c> / <c>MetaKeywords</c> /
///     <c>OgImageUrl</c> / <c>CanonicalUrl</c> were wiped on every General
///     save because the General form doesn't post them and SaveCoreAsync was
///     unconditionally assigning the freshly-defaulted null values.
///   - The edit form had no validation span for <c>Input.Category</c>.
///
/// Each test below simulates the exact production POST path through
/// <see cref="EditorModel"/> (handler → SaveCoreAsync → repository →
/// SaveChangesAsync) and asserts the persistence outcome.
/// </summary>
public class MotorcycleEditRegressionTests : IDisposable
{
    private readonly HieuNgaDbContext _db;
    private readonly IRepository<Motorcycle> _motorcycleRepo;
    private readonly IRepository<MotorcycleVariant> _variantRepo;
    private readonly IUnitOfWork _uow;
    private readonly Mock<IImageStorageService> _imageStorage = new();
    private readonly Mock<IMotorcycleMediaStudioService> _mediaStudio = new();
    private readonly Mock<IImageUploadService> _imageUploader = new();

    public MotorcycleEditRegressionTests()
    {
        var opts = new DbContextOptionsBuilder<HieuNgaDbContext>()
            .UseInMemoryDatabase($"MotorcycleEditRegressionTests-{Guid.NewGuid():N}")
            .Options;
        _db = new HieuNgaDbContext(opts);
        _motorcycleRepo = new Repository<Motorcycle>(_db);
        _variantRepo = new Repository<MotorcycleVariant>(_db);
        _uow = new UnitOfWork(_db);

        _imageStorage.SetupGet(s => s.SupportsUpload).Returns(false);
        _imageStorage.SetupGet(s => s.StorageDescription).Returns("test-storage");
        _imageUploader.SetupGet(u => u.SupportedKinds)
            .Returns(new HashSet<string>(StringComparer.Ordinal));
    }

    public void Dispose() => _db.Dispose();

    private EditorModel CreatePage(Motorcycle? seed = null)
    {
        if (seed is not null)
        {
            _db.Motorcycles.Add(seed);
            _db.SaveChanges();
        }

        var page = new EditorModel(
            _motorcycleRepo, _variantRepo, _uow, _db,
            _imageStorage.Object, _mediaStudio.Object, _imageUploader.Object,
            NullLogger<EditorModel>.Instance);

        var http = new DefaultHttpContext { TraceIdentifier = "test-trace" };
        var actionContext = new ActionContext(http, new RouteData(), new PageActionDescriptor(), page.ModelState);
        var pageContext = new PageContext(actionContext)
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), page.ModelState)
        };
        page.PageContext = pageContext;
        page.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        return page;
    }

    private static Motorcycle SeedBike(string name = "Airblade Marvel", decimal basePrice = 41_290_000m) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = SlugFromName(name),
        Category = MotorcycleCategory.Scooter,
        BasePrice = basePrice,
        ShortDescription = "Xe ga thể thao",
        Description = "Mô tả dài ban đầu",
        IsFeatured = false,
        IsPublished = true,
        SortOrder = 5,
        ThumbnailUrl = "https://res.cloudinary.com/demo/seed.jpg",
        MetaTitle = "Meta Title gốc",
        MetaDescription = "Meta Description gốc",
        MetaKeywords = "honda, airblade",
        OgImageUrl = "https://res.cloudinary.com/demo/seed-og.jpg",
        CanonicalUrl = "/xe/airblade-marvel",
    };

    private static string SlugFromName(string name) =>
        name.ToLowerInvariant().Replace(' ', '-');

    // ---------- Motorbike field-level persistence ----------

    [Fact]
    public async Task Edit_BasePrice_Persists()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.BasePrice = 39_990_000m;
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(39_990_000m, reloaded.BasePrice);
    }

    [Fact]
    public async Task Edit_Category_Persists()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.Category = MotorcycleCategory.ConTay;
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(MotorcycleCategory.ConTay, reloaded.Category);
    }

    [Fact]
    public async Task Edit_Name_Persists()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.Name = "Airblade Marvel 2026";
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal("Airblade Marvel 2026", reloaded.Name);
    }

    [Fact]
    public async Task Edit_ShortDescription_Persists()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.ShortDescription = "Mô tả ngắn mới";
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal("Mô tả ngắn mới", reloaded.ShortDescription);
    }

    [Fact]
    public async Task Edit_Description_Persists()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.Description = "Mô tả dài mới";
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal("Mô tả dài mới", reloaded.Description);
    }

    [Fact]
    public async Task Edit_SortOrder_Persists()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.SortOrder = 99;
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(99, reloaded.SortOrder);
    }

    [Fact]
    public async Task Edit_IsFeatured_Persists()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.IsFeatured = true;
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.True(reloaded.IsFeatured);
    }

    [Fact]
    public async Task Edit_IsPublished_True_Persists()
    {
        // Regression: previously, SaveCoreAsync called ApplyPublishStatusToInput
        // which forced Input.IsPublished=false for every General save
        // (PublishStatus wasn't posted from General so it defaulted to "draft").
        // Saving from General must respect the checkbox.
        var seed = SeedBike();
        seed.IsPublished = false;
        _db.SaveChanges();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.IsPublished = true;
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.True(reloaded.IsPublished,
            "Saving from the General tab with the IsPublished checkbox checked must persist the published state.");
    }

    [Fact]
    public async Task Edit_IsPublished_False_Persists_WithoutUnPublishingSurprise()
    {
        // Regression: a published motorcycle must remain published when the
        // admin saves from the General tab without touching the publish checkbox.
        // The previous ApplyPublishStatusToInput call flipped it to false.
        var seed = SeedBike();
        seed.IsPublished = true;
        _db.SaveChanges();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        // Admin only changes the BasePrice from General tab.
        page.Input.BasePrice = 42_000_000m;
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.True(reloaded.IsPublished,
            "Saving the General tab must not silently un-publish the motorcycle.");
        Assert.Equal(42_000_000m, reloaded.BasePrice);
    }

    [Fact]
    public async Task Edit_MultipleGeneralFields_AllPersist()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.Name = "Vision 2026";
        page.Input.BasePrice = 32_500_000m;
        page.Input.Category = MotorcycleCategory.PhanKhoiLon;
        page.Input.ShortDescription = "Mới";
        page.Input.Description = "Mới dài";
        page.Input.IsFeatured = true;
        page.Input.SortOrder = 42;

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal("Vision 2026", reloaded.Name);
        Assert.Equal(32_500_000m, reloaded.BasePrice);
        Assert.Equal(MotorcycleCategory.PhanKhoiLon, reloaded.Category);
        Assert.Equal("Mới", reloaded.ShortDescription);
        Assert.Equal("Mới dài", reloaded.Description);
        Assert.True(reloaded.IsFeatured);
        Assert.Equal(42, reloaded.SortOrder);
    }

    // ---------- General save must not wipe SEO ----------

    [Fact]
    public async Task Edit_GeneralSave_DoesNotWipeSeoFields()
    {
        // Regression: SaveCoreAsync unconditionally overwrote MetaTitle /
        // MetaDescription / MetaKeywords / OgImageUrl / CanonicalUrl from
        // Input.* which defaulted to null because the General tab form
        // doesn't post them. Every General save used to wipe SEO data.
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        // Admin only edits a general field.
        page.Input.BasePrice = 39_990_000m;
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(39_990_000m, reloaded.BasePrice);
        Assert.Equal("Meta Title gốc", reloaded.MetaTitle);
        Assert.Equal("Meta Description gốc", reloaded.MetaDescription);
        Assert.Equal("honda, airblade", reloaded.MetaKeywords);
        Assert.Equal("https://res.cloudinary.com/demo/seed-og.jpg", reloaded.OgImageUrl);
        Assert.Equal("/xe/airblade-marvel", reloaded.CanonicalUrl);
    }

    // ---------- No duplicates ----------

    [Fact]
    public async Task Edit_DoesNotCreateDuplicateMotorcycle()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.BasePrice = 55_000_000m;
        await page.OnPostSaveGeneralAsync(CancellationToken.None);

        // The repository must not have added a new row.
        Assert.Single(await _db.Motorcycles.AsNoTracking().ToListAsync());
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(55_000_000m, reloaded.BasePrice);
    }

    [Fact]
    public async Task Edit_DoesNotCreateDuplicateVariant()
    {
        var seed = SeedBike();
        var variantId = Guid.NewGuid();
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = variantId,
            MotorcycleId = seed.Id,
            Name = "Tiêu chuẩn",
            Slug = "tieu-chuan",
            Price = 41_290_000m,
            StockQuantity = 10,
            IsAvailable = true,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Id = variantId,
            Name = "Tiêu chuẩn",
            Price = 42_000_000m,
            StockQuantity = 8,
            IsAvailable = false,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        // Only the original row, with the new values.
        var variants = await _db.MotorcycleVariants.AsNoTracking()
            .Where(v => v.MotorcycleId == seed.Id).ToListAsync();
        Assert.Single(variants);
        Assert.Equal(42_000_000m, variants[0].Price);
        Assert.Equal(8, variants[0].StockQuantity);
        Assert.False(variants[0].IsAvailable);
    }

    // ---------- Variant edit persistence ----------

    [Fact]
    public async Task EditVariant_Price_Persists()
    {
        var seed = SeedBike();
        var variantId = Guid.NewGuid();
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = variantId,
            MotorcycleId = seed.Id,
            Name = "Cao cấp",
            Slug = "cao-cap",
            Price = 45_000_000m,
            StockQuantity = 5,
            IsAvailable = true,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Id = variantId,
            Name = "Cao cấp",
            Price = 46_500_000m,
            StockQuantity = 5,
            IsAvailable = true,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.MotorcycleVariants.AsNoTracking().FirstAsync(v => v.Id == variantId);
        Assert.Equal(46_500_000m, reloaded.Price);
    }

    [Fact]
    public async Task EditVariant_StockQuantity_Persists()
    {
        var seed = SeedBike();
        var variantId = Guid.NewGuid();
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = variantId,
            MotorcycleId = seed.Id,
            Name = "Cao cấp",
            Slug = "cao-cap",
            Price = 45_000_000m,
            StockQuantity = 5,
            IsAvailable = true,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Id = variantId,
            Name = "Cao cấp",
            Price = 45_000_000m,
            StockQuantity = 17,
            IsAvailable = true,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.MotorcycleVariants.AsNoTracking().FirstAsync(v => v.Id == variantId);
        Assert.Equal(17, reloaded.StockQuantity);
    }

    [Fact]
    public async Task EditVariant_Name_Persists()
    {
        var seed = SeedBike();
        var variantId = Guid.NewGuid();
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = variantId,
            MotorcycleId = seed.Id,
            Name = "Cũ",
            Slug = "cu",
            Price = 45_000_000m,
            StockQuantity = 5,
            IsAvailable = true,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Id = variantId,
            Name = "Đặc biệt",
            Price = 45_000_000m,
            StockQuantity = 5,
            IsAvailable = true,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.MotorcycleVariants.AsNoTracking().FirstAsync(v => v.Id == variantId);
        Assert.Equal("Đặc biệt", reloaded.Name);
    }

    [Fact]
    public async Task EditVariant_IsAvailable_Persists()
    {
        var seed = SeedBike();
        var variantId = Guid.NewGuid();
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = variantId,
            MotorcycleId = seed.Id,
            Name = "Cao cấp",
            Slug = "cao-cap",
            Price = 45_000_000m,
            StockQuantity = 5,
            IsAvailable = true,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Id = variantId,
            Name = "Cao cấp",
            Price = 45_000_000m,
            StockQuantity = 5,
            IsAvailable = false,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.MotorcycleVariants.AsNoTracking().FirstAsync(v => v.Id == variantId);
        Assert.False(reloaded.IsAvailable);
    }

    [Fact]
    public async Task EditVariant_EnteringUpdateBranch_PersistsSlugStably()
    {
        // Slug must remain stable across edits so the public detail URL
        // never breaks.
        var seed = SeedBike();
        var variantId = Guid.NewGuid();
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = variantId,
            MotorcycleId = seed.Id,
            Name = "Cao cấp",
            Slug = "cao-cap",
            Price = 45_000_000m,
            StockQuantity = 5,
            IsAvailable = true,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Id = variantId,
            Name = "Cao cấp Plus",
            Price = 48_000_000m,
            StockQuantity = 5,
            IsAvailable = true,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Đã cập nhật phiên bản.", page.TempData["AdminSuccess"]);

        var reloaded = await _db.MotorcycleVariants.AsNoTracking().FirstAsync(v => v.Id == variantId);
        Assert.Equal("Cao cấp Plus", reloaded.Name);
        Assert.Equal(48_000_000m, reloaded.Price);
        Assert.Equal("cao-cap", reloaded.Slug);
    }

    // ---------- Access / safety ----------

    [Fact]
    public async Task Edit_SoftDeletedMotorcycle_ReturnsNotFound()
    {
        var seed = SeedBike();
        seed.IsDeleted = true;
        _db.SaveChanges();

        var page = CreatePage();
        page.Id = seed.Id;
        page.Input = new MotorcycleInputModel
        {
            Name = "Should fail",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        page.Tab = "general";

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_InvalidId_ReturnsNotFound()
    {
        var page = CreatePage();
        page.Id = Guid.NewGuid(); // not seeded
        page.Input = new MotorcycleInputModel
        {
            Name = "Should fail",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        page.Tab = "general";

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- After save, GET reload reflects the persisted values ----------

    [Fact]
    public async Task Edit_GetAfterPost_ShowsPersistedValues()
    {
        // End-to-end: admin saves → redirects to GET → form re-renders
        // with the new DB values (not stale cached data).
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.BasePrice = 39_990_000m;
        page.Input.Category = MotorcycleCategory.XeSo;
        await page.OnPostSaveGeneralAsync(CancellationToken.None);

        // New GET on a fresh page instance must reflect the saved values.
        var page2 = CreatePage();
        page2.Id = seed.Id;
        await page2.OnGetAsync(null, CancellationToken.None);

        Assert.Equal(39_990_000m, page2.Input.BasePrice);
        Assert.Equal(MotorcycleCategory.XeSo, page2.Input.Category);
    }

    // ---------- CRITICAL TEST: Category + Price persistence ----------

    [Fact]
    public async Task Edit_ChangeCategoryAndPrice_BothPersist_AndSurviveReload()
    {
        // Exact production scenario from the bug report:
        //   1. Existing:  Category=Scooter,  Price=oldValue
        //   2. Edit:      Category=ConTay,  Price=newValue
        //   3. Save       → HTTP 200 (RedirectToPage)
        //   4. Reload from DB → Category=ConTay, Price=newValue
        const decimal oldPrice = 41_290_000m;
        const decimal newPrice = 50_500_000m;

        var seed = SeedBike(basePrice: oldPrice); // Category=Scooter by default
        Assert.Equal(MotorcycleCategory.Scooter, seed.Category);
        Assert.Equal(oldPrice, seed.BasePrice);

        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        // Simulate the admin changing Category + Price in the form.
        page.Input.Category = MotorcycleCategory.ConTay;
        page.Input.BasePrice = newPrice;

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        // Step 3: must not be a 200 page-result; the production handler
        // redirects after a successful save.
        Assert.IsType<RedirectToPageResult>(result);

        // Step 4: reload from the same context with AsNoTracking → fresh
        // snapshot from the DB, no in-memory caching.
        var reloaded = await _db.Motorcycles.AsNoTracking()
            .FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(MotorcycleCategory.ConTay, reloaded.Category);
        Assert.Equal(newPrice, reloaded.BasePrice);

        // Step 5: another AsNoTracking read to prove stability.
        var freshRead = await _db.Motorcycles.AsNoTracking()
            .FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(MotorcycleCategory.ConTay, freshRead.Category);
        Assert.Equal(newPrice, freshRead.BasePrice);

        // Other fields must remain untouched (no collateral damage).
        Assert.Equal(seed.Name, freshRead.Name);
        Assert.Equal(seed.ShortDescription, freshRead.ShortDescription);
        Assert.Equal(seed.Description, freshRead.Description);
        Assert.Equal(seed.IsFeatured, freshRead.IsFeatured);
        Assert.Equal(seed.IsPublished, freshRead.IsPublished);
        Assert.Equal(seed.SortOrder, freshRead.SortOrder);
        Assert.Equal(seed.ThumbnailUrl, freshRead.ThumbnailUrl);
    }

    [Fact]
    public async Task Edit_ChangeCategoryOnly_Persists_AndPriceUnchanged()
    {
        var seed = SeedBike(basePrice: 41_290_000m);
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.Category = MotorcycleCategory.PhanKhoiLon;
        // BasePrice intentionally NOT touched.

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(MotorcycleCategory.PhanKhoiLon, reloaded.Category);
        Assert.Equal(41_290_000m, reloaded.BasePrice); // unchanged
    }

    [Fact]
    public async Task Edit_ChangePriceOnly_Persists_AndCategoryUnchanged()
    {
        var seed = SeedBike(); // Category=Scooter
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.BasePrice = 39_990_000m;
        // Category intentionally NOT touched.

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(39_990_000m, reloaded.BasePrice);
        Assert.Equal(MotorcycleCategory.Scooter, reloaded.Category); // unchanged
    }

    [Fact]
    public async Task Edit_NoChanges_Succeeds_AndPersistsUntouched()
    {
        // Admin opens Edit, makes no changes, hits Save. The handler must
        // redirect (200) without disturbing any field.
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(seed.Name, reloaded.Name);
        Assert.Equal(seed.Category, reloaded.Category);
        Assert.Equal(seed.BasePrice, reloaded.BasePrice);
        Assert.Equal(seed.ShortDescription, reloaded.ShortDescription);
        Assert.Equal(seed.Description, reloaded.Description);
        Assert.Equal(seed.IsFeatured, reloaded.IsFeatured);
        Assert.Equal(seed.IsPublished, reloaded.IsPublished);
        Assert.Equal(seed.SortOrder, reloaded.SortOrder);
        Assert.Equal(seed.ThumbnailUrl, reloaded.ThumbnailUrl);
    }

    [Fact]
    public async Task Edit_DecimalBasePrice_WithSubVndAmounts_PersistsExactly()
    {
        // BasePrice is decimal. Verify sub-đồng values (theoretical, since
        // VNĐ has no fractional unit but the type allows it) and values that
        // step outside round thousands — both must round-trip through EF
        // without truncation.
        var seed = SeedBike(basePrice: 41_290_000m);
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.BasePrice = 32_500_500.75m; // sub-thousand fractional
        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(32_500_500.75m, reloaded.BasePrice);

        // A second value with different fractional digits.
        page.Input.BasePrice = 18_750_000.99m;
        await page.OnPostSaveGeneralAsync(CancellationToken.None);
        var reloaded2 = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(18_750_000.99m, reloaded2.BasePrice);
    }

    [Fact]
    public async Task Edit_KeepExistingThumbnail_WhenNoNewFileUploaded()
    {
        // Save without uploading a thumbnail must NOT wipe the stored one.
        // The production handler guards this with the
        //   `if (uploadedUrl is not null || !string.IsNullOrWhiteSpace(Input.ThumbnailUrl))`
        // condition; this test pins the behavior.
        var seed = SeedBike();
        const string existingThumb = "https://res.cloudinary.com/demo/seed.jpg";
        Assert.Equal(existingThumb, seed.ThumbnailUrl);

        var page = CreatePage(seed);
        page.Id = seed.Id;
        // ThumbnailFile is null (no upload) — we do not assign it.
        page.ThumbnailFile = null;
        await page.OnGetAsync(null, CancellationToken.None);

        // Admin edits Category + Price only.
        page.Input.Category = MotorcycleCategory.Electric;
        page.Input.BasePrice = 99_000_000m;

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(existingThumb, reloaded.ThumbnailUrl);
        Assert.Equal(MotorcycleCategory.Electric, reloaded.Category);
        Assert.Equal(99_000_000m, reloaded.BasePrice);
    }

    // ---------- Silent validation regression guards ----------

    [Fact]
    public async Task Edit_InvalidCategory_DoesNotPersistOtherFields()
    {
        // Regression: when the General-tab form posted an empty Category value
        // (the "-- Chọn loại xe --" placeholder option), the default model
        // binder could not convert "" to the enum, so ModelState went invalid
        // and SaveCoreAsync never ran — yet the user saw their typed
        // BasePrice reflected back from the model binder and thought the save
        // had worked. This guards the silent failure: when ModelState is
        // invalid, nothing must hit the DB.
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.BasePrice = 99_000_000m;
        page.Input.Name = "Should not be saved";
        // Simulate the binder behavior when "" is posted for an enum field.
        page.ModelState.AddModelError("Input.Category", "The value '' is not valid for Input.Category.");

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(41_290_000m, reloaded.BasePrice);
        Assert.Equal("Airblade Marvel", reloaded.Name);
    }

    [Fact]
    public async Task Edit_InvalidBasePrice_DoesNotPersistOtherFields()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.BasePrice = -1m; // out of [Range]
        page.Input.Category = MotorcycleCategory.ConTay;
        page.ModelState.AddModelError("Input.BasePrice",
            "The field BasePrice must be between 0 and 1.7976931348623157E+308.");

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal(41_290_000m, reloaded.BasePrice);
        Assert.Equal(MotorcycleCategory.Scooter, reloaded.Category);
    }

    [Fact]
    public async Task Edit_EmptyName_DoesNotPersistOtherFields()
    {
        var seed = SeedBike();
        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);

        page.Input.Name = "";
        page.Input.BasePrice = 99_000_000m;
        page.ModelState.AddModelError("Input.Name", "Vui lòng nhập tên xe");

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.Equal("Airblade Marvel", reloaded.Name);
        Assert.Equal(41_290_000m, reloaded.BasePrice);
    }

    // ---------- Publish tab still uses PublishStatus as source of truth ----------

    [Fact]
    public async Task EditPublish_SettingPublished_SetsIsPublishedTrue()
    {
        // The Publish tab uses radio buttons for PublishStatus. The handler
        // must translate "published" → Input.IsPublished=true and persist it.
        var seed = SeedBike();
        seed.IsPublished = false;
        _db.SaveChanges();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);
        page.PublishStatus = "published";
        page.Input.IsFeatured = true;
        page.Input.SortOrder = 10;

        var result = await page.OnPostSavePublishAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.True(reloaded.IsPublished);
        Assert.True(reloaded.IsFeatured);
        Assert.Equal(10, reloaded.SortOrder);
    }

    [Fact]
    public async Task EditPublish_Archiving_ClearsIsPublishedAndIsFeatured()
    {
        var seed = SeedBike();
        seed.IsPublished = true;
        seed.IsFeatured = true;
        _db.SaveChanges();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        await page.OnGetAsync(null, CancellationToken.None);
        page.PublishStatus = "archived";

        var result = await page.OnPostSavePublishAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(result);

        var reloaded = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.False(reloaded.IsPublished);
        Assert.False(reloaded.IsFeatured);
    }
}