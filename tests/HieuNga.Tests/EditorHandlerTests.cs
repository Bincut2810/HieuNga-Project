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
using System.Text.Json;

namespace HieuNga.Tests;

/// <summary>
/// Integration-flavored unit tests for the EditorModel PageModel. They exercise
/// the actual handler methods (OnPostSaveGeneralAsync, OnPostSaveSpecsAsync,
/// OnPostSaveVariantAsync, OnPostDeleteVariantAsync) against an InMemory
/// EF Core database with real <see cref="IRepository{T}"/> /
/// <see cref="IUnitOfWork"/> instances.
///
/// The PageModel is constructed directly; <c>HttpContext</c>, <c>ModelState</c>
/// and <c>RouteData</c> are wired up by hand so we can simulate the exact
/// ModelState pollution observed in production (unprefixed nested keys
/// "Name" / "Title") without spinning up the full MVC pipeline.
/// </summary>
public class EditorHandlerTests : IDisposable
{
    private readonly HieuNgaDbContext _db;
    private readonly IRepository<Motorcycle> _motorcycleRepo;
    private readonly IRepository<MotorcycleVariant> _variantRepo;
    private readonly IUnitOfWork _uow;
    private readonly Mock<IImageStorageService> _imageStorage = new();
    private readonly Mock<IMotorcycleMediaStudioService> _mediaStudio = new();
    private readonly Mock<IImageUploadService> _imageUploader = new();

    public EditorHandlerTests()
    {
        var opts = new DbContextOptionsBuilder<HieuNgaDbContext>()
            .UseInMemoryDatabase($"EditorHandlerTests-{Guid.NewGuid():N}")
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

    public void Dispose()
    {
        _db.Dispose();
    }

    private EditorModel CreatePage(Motorcycle? seed = null)
    {
        if (seed is not null)
        {
            _db.Motorcycles.Add(seed);
            _db.SaveChanges();
        }

        var page = new EditorModel(
            _motorcycleRepo,
            _variantRepo,
            _uow,
            _db,
            _imageStorage.Object,
            _mediaStudio.Object,
            _imageUploader.Object,
            NullLogger<EditorModel>.Instance);

        // PageModel's default ctor initializes ViewData / TempData / ModelState
        // (non-null), but HttpContext / RouteData are framework-injected.
        var http = new DefaultHttpContext { TraceIdentifier = "test-trace" };
        var actionContext = new ActionContext(http, new RouteData(), new PageActionDescriptor(), page.ModelState);
        var pageContext = new PageContext(actionContext);

        // PageModel.ViewData reads from PageContext.ViewData (which lazy-inits
        // to a new ViewDataDictionary on first access in .NET 8). Wire a real
        // ViewDataDictionary so SetViewData() can write CategoryOptions etc.
        pageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), page.ModelState);
        page.PageContext = pageContext;

        // TempData requires a backing store for the SetSuccess/SetError helpers.
        page.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        return page;
    }

    private static void PolluteWithNestedModelErrors(ModelStateDictionary ms, string keyShape)
    {
        // keyShape controls whether the binding pipeline emits prefixed
        // ("VariantForm.Name") or unprefixed ("Name") ModelState keys.
        // Both shapes are observed across ASP.NET Core versions; production
        // emits the unprefixed shape.
        if (keyShape == "prefixed")
        {
            ms.AddModelError("VariantForm.Name", "Vui lòng nhập tên phiên bản");
            ms.AddModelError("VariantForm.Price", "out of range");
            ms.AddModelError("NewFeature.Title", "The Title field is required.");
            ms.AddModelError("NewTech.Title", "The Title field is required.");
        }
        else
        {
            ms.AddModelError("Name", "Vui lòng nhập tên phiên bản");
            ms.AddModelError("Title", "The Title field is required.");
        }
    }

    // ---------- TASK 1: SaveGeneral ----------

    [Fact]
    public async Task SaveGeneral_WithValidInput_CreatesMotorcycle()
    {
        var page = CreatePage();
        page.Input = new MotorcycleInputModel
        {
            Name = "Vision 2026",
            Slug = "vision-2026",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 30_000_000m,
            ThumbnailUrl = "https://res.cloudinary.com/demo/vision.jpg",
        };
        page.Tab = "general";
        page.PublishStatus = "draft";

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("media", redirect.RouteValues!["tab"]);

        var bike = await _db.Motorcycles.AsNoTracking().FirstOrDefaultAsync(m => m.Slug == "vision-2026");
        Assert.NotNull(bike);
        Assert.Equal("Vision 2026", bike!.Name);
        Assert.Equal(MotorcycleCategory.Scooter, bike.Category);
        Assert.Equal(30_000_000m, bike.BasePrice);
        Assert.Equal("https://res.cloudinary.com/demo/vision.jpg", bike.ThumbnailUrl);
        Assert.False(bike.IsPublished);
    }

    [Fact]
    public async Task SaveGeneral_WithoutVariantFormName_StillCreatesMotorcycle()
    {
        var page = CreatePage();
        page.Input = new MotorcycleInputModel
        {
            Name = "SH Mode",
            Slug = "sh-mode",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 25_000_000m,
            ThumbnailUrl = "https://res.cloudinary.com/demo/sh.jpg",
        };
        page.Tab = "general";
        page.PublishStatus = "draft";

        // Simulate the production ModelState pollution: unprefixed nested key.
        page.ModelState.AddModelError("Name", "Vui lòng nhập tên phiên bản");

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.True(page.ModelState.IsValid,
            "After RemoveAllExcept, the VariantForm pollution must be gone so SaveCoreAsync reaches commit.");
        Assert.NotNull(await _db.Motorcycles.AsNoTracking().FirstOrDefaultAsync(m => m.Slug == "sh-mode"));
    }

    [Fact]
    public async Task SaveGeneral_WithoutNewFeatureTitle_StillCreatesMotorcycle()
    {
        var page = CreatePage();
        page.Input = new MotorcycleInputModel
        {
            Name = "Air Blade",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 22_000_000m,
            ThumbnailUrl = "https://res.cloudinary.com/demo/airblade.jpg",
        };
        page.Tab = "general";

        page.ModelState.AddModelError("Title", "The Title field is required.");

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.True(page.ModelState.IsValid);
        var bike = await _db.Motorcycles.AsNoTracking().FirstOrDefaultAsync(m => m.Name == "Air Blade");
        Assert.NotNull(bike);
    }

    [Fact]
    public async Task SaveGeneral_WithoutNewTechTitle_StillCreatesMotorcycle()
    {
        var page = CreatePage();
        page.Input = new MotorcycleInputModel
        {
            Name = "Winner X",
            Category = MotorcycleCategory.XeSo,
            BasePrice = 18_000_000m,
            ThumbnailUrl = "https://res.cloudinary.com/demo/winner.jpg",
        };
        page.Tab = "general";

        // Triple pollution: VariantForm.Name + NewFeature.Title + NewTech.Title
        page.ModelState.AddModelError("Name", "Vui lòng nhập tên phiên bản");
        page.ModelState.AddModelError("Title", "The Title field is required.");

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.True(page.ModelState.IsValid);
        var bike = await _db.Motorcycles.AsNoTracking().FirstOrDefaultAsync(m => m.Name == "Winner X");
        Assert.NotNull(bike);
    }

    [Theory]
    [InlineData("prefixed")]
    [InlineData("unprefixed")]
    public async Task SaveGeneral_IsRobustToEitherKeyShape(string keyShape)
    {
        var page = CreatePage();
        page.Input = new MotorcycleInputModel
        {
            Name = $"Test-{keyShape}",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 10_000_000m,
            ThumbnailUrl = "https://res.cloudinary.com/demo/x.jpg",
        };
        page.Tab = "general";
        PolluteWithNestedModelErrors(page.ModelState, keyShape);

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.True(page.ModelState.IsValid);
        Assert.NotNull(await _db.Motorcycles.AsNoTracking().FirstOrDefaultAsync(m => m.Name == $"Test-{keyShape}"));
    }

    [Fact]
    public async Task SaveGeneral_InputNameMissing_StopsBeforeCommit()
    {
        var page = CreatePage();
        page.Input = new MotorcycleInputModel
        {
            // Name intentionally left blank — [Required] must still fire.
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
            ThumbnailUrl = "https://res.cloudinary.com/demo/x.jpg",
        };
        page.Tab = "general";

        // Real validation fires for Input.Name (whitelist keeps Input.*).
        page.ModelState.AddModelError("Input.Name", "Vui lòng nhập tên xe");

        var result = await page.OnPostSaveGeneralAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.Empty(await _db.Motorcycles.ToListAsync());
    }

    // ---------- TASK 2: SaveSpecs ----------

    [Fact]
    public async Task SaveSpecs_PersistsTechnicalSpecsJson()
    {
        var seed = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Spec Bike",
            Slug = "spec-bike",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.SpecsLines = "## Động cơ\nCông suất|9.6 kW\nMô-men xoắn|12.5 Nm";

        var result = await page.OnPostSaveSpecsAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("specifications", redirect.RouteValues!["tab"]);

        var saved = await _db.Motorcycles.AsNoTracking().FirstAsync(m => m.Id == seed.Id);
        Assert.NotNull(saved.TechnicalSpecsJson);

        // Default JsonSerializer escapes non-ASCII; deserialize to assert content
        // rather than the wire format.
        using var doc = JsonDocument.Parse(saved.TechnicalSpecsJson!);
        var items = doc.RootElement.EnumerateArray()
            .Select(e => new
            {
                Icon = e.GetProperty("Icon").GetString(),
                Label = e.GetProperty("Label").GetString(),
                Value = e.GetProperty("Value").GetString(),
            })
            .ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal("group", items[0].Icon);
        Assert.Equal("Động cơ", items[0].Label);
        Assert.Equal("Công suất", items[1].Label);
        Assert.Equal("9.6 kW", items[1].Value);
        Assert.Equal("Mô-men xoắn", items[2].Label);
        Assert.Equal("12.5 Nm", items[2].Value);
    }

    [Fact]
    public async Task SaveSpecs_OnCreate_RedirectsToGeneral()
    {
        var page = CreatePage();
        page.SpecsLines = "ignored";

        var result = await page.OnPostSaveSpecsAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("general", redirect.RouteValues!["tab"]);
    }

    // ---------- TASK 3: SaveVariant / UpdateVariant / DeleteVariant ----------

    [Fact]
    public async Task SaveVariant_New_CreatesVariant()
    {
        var seed = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Variant Bike",
            Slug = "variant-bike",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Name = "Tiêu chuẩn",
            Price = 30_000_000m,
            StockQuantity = 5,
            IsAvailable = true,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("finance", redirect.RouteValues!["tab"]);
        Assert.Equal("Đã thêm phiên bản.", page.TempData["AdminSuccess"]);

        var variant = await _db.MotorcycleVariants.AsNoTracking().FirstAsync(v => v.MotorcycleId == seed.Id);
        Assert.Equal("Tiêu chuẩn", variant.Name);
        Assert.Equal("tieu-chuan", variant.Slug);
        Assert.Equal(30_000_000m, variant.Price);
        Assert.Equal(5, variant.StockQuantity);
        Assert.True(variant.IsAvailable);
    }

    [Fact]
    public async Task SaveVariant_New_DuplicateName_AppendsCounterSlug()
    {
        var seed = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Dup Bike",
            Slug = "dup-bike",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            MotorcycleId = seed.Id,
            Name = "Cao cấp",
            Slug = "cao-cap",
            Price = 1m,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Name = "Cao cấp",
            Price = 35_000_000m,
            StockQuantity = 1,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        var allVariants = await _db.MotorcycleVariants.AsNoTracking()
            .Where(v => v.MotorcycleId == seed.Id)
            .OrderBy(v => v.Slug)
            .ToListAsync();
        Assert.Equal(2, allVariants.Count);
        Assert.Contains(allVariants, v => v.Slug == "cao-cap");
        Assert.Contains(allVariants, v => v.Slug == "cao-cap-2");
    }

    [Fact]
    public async Task SaveVariant_Update_UpdatesExistingVariant()
    {
        var seed = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Update Bike",
            Slug = "update-bike",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        var variantId = Guid.NewGuid();
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = variantId,
            MotorcycleId = seed.Id,
            Name = "Bản cũ",
            Slug = "ban-cu",
            Price = 10_000_000m,
            StockQuantity = 1,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;
        page.VariantForm = new EditorModel.VariantFormInput
        {
            Id = variantId,
            Name = "Bản mới",
            Price = 99_000_000m,
            StockQuantity = 42,
            IsAvailable = false,
        };

        var result = await page.OnPostSaveVariantAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Đã cập nhật phiên bản.", page.TempData["AdminSuccess"]);
        var saved = await _db.MotorcycleVariants.AsNoTracking().FirstAsync(v => v.Id == variantId);
        Assert.Equal("Bản mới", saved.Name);
        Assert.Equal(99_000_000m, saved.Price);
        Assert.Equal(42, saved.StockQuantity);
        Assert.False(saved.IsAvailable);
        // Slug is preserved on edit so the public detail link stays valid.
        Assert.Equal("ban-cu", saved.Slug);
    }

    [Fact]
    public async Task DeleteVariant_SoftDeletes()
    {
        var seed = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Delete Bike",
            Slug = "delete-bike",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        var variantId = Guid.NewGuid();
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = variantId,
            MotorcycleId = seed.Id,
            Name = "Sắp xóa",
            Slug = "sap-xoa",
            Price = 1m,
        });
        await _db.SaveChangesAsync();

        var page = CreatePage(seed);
        page.Id = seed.Id;

        var result = await page.OnPostDeleteVariantAsync(variantId, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Đã xóa phiên bản.", page.TempData["AdminSuccess"]);

        // Soft-delete: row still exists with IsDeleted = true and the
        // global query filter hides it from the default query path.
        var raw = await _db.MotorcycleVariants.IgnoreQueryFilters().FirstAsync(v => v.Id == variantId);
        Assert.True(raw.IsDeleted);

        Assert.Empty(await _db.MotorcycleVariants.ToListAsync());
    }

    [Fact]
    public async Task DeleteVariant_OtherMotoVariant_ReturnsNotFound()
    {
        var seed = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Owner Bike",
            Slug = "owner-bike",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        var stranger = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Stranger Bike",
            Slug = "stranger-bike",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 1m,
        };
        var strangerVariantId = Guid.NewGuid();
        _db.Motorcycles.AddRange(seed, stranger);
        _db.MotorcycleVariants.Add(new MotorcycleVariant
        {
            Id = strangerVariantId,
            MotorcycleId = stranger.Id,
            Name = "Không phải của tôi",
            Slug = "khong-phai-cua-toi",
            Price = 1m,
        });
        await _db.SaveChangesAsync();

        // CreatePage re-adds the seed if we pass it; seed is already in the DB,
        // so we use the parameterless overload and wire Id manually.
        var page = CreatePage();
        page.Id = seed.Id;

        var result = await page.OnPostDeleteVariantAsync(strangerVariantId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        // The stranger's variant must remain untouched.
        var stillThere = await _db.MotorcycleVariants.IgnoreQueryFilters().FirstAsync(v => v.Id == strangerVariantId);
        Assert.False(stillThere.IsDeleted);
    }
}
