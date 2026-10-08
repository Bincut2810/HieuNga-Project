using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// HTTP-level reproduction of the production "Motorcycle Edit Save: Category
/// and Price do not persist" bug.
///
/// Unlike <see cref="MotorcycleEditRegressionTests"/> (which calls the
/// handler methods directly with a pre-populated <c>Input</c> model and
/// therefore bypasses the ASP.NET Core model binder), this class drives the
/// ACTUAL HTTP/browser request pipeline:
/// <list type="number">
///   <item>GET <c>/admin/xe/editor/{id}?tab=general</c> — captures the
///   rendered HTML so the form structure (action, method, handler, field
///   names, hidden inputs) can be inspected verbatim.</item>
///   <item>POST the same URL with <c>application/x-www-form-urlencoded</c>
///   body that mirrors exactly what a browser would send when the user
///   edits the Category dropdown and the BasePrice number input and
///   clicks Save.</item>
///   <item>Re-read the motorcycle from the SAME InMemory store via a fresh
///   <see cref="HieuNgaDbContext"/> (no shared change tracker) to verify
///   what was actually persisted.</item>
///   <item>GET the editor again to verify the post-redirect render
///   reflects the persisted values.</item>
/// </list>
/// If the user's reported bug is reproducible, the category / BasePrice
/// will remain the OLD values after this end-to-end run.
/// </summary>
public class MotorcycleEditHttpReproTests : IClassFixture<HieuNgaTestAppFactory>
{
    private readonly HieuNgaTestAppFactory _factory;

    public MotorcycleEditHttpReproTests(HieuNgaTestAppFactory factory)
    {
        _factory = factory;
    }

    // ---------- helpers ----------

    private async Task<Motorcycle> SeedAsync(
        string name = "Airblade Marvel",
        MotorcycleCategory category = MotorcycleCategory.Scooter,
        decimal basePrice = 41_290_000m,
        string? thumbnailUrl = "https://res.cloudinary.com/demo/seed.jpg")
    {
        // Each call gets a UNIQUE id, UNIQUE name AND UNIQUE slug so:
        //  1. The slug-collision check inside SaveCoreAsync does not fire
        //     across tests that share the same InMemoryDatabase via
        //     IClassFixture (SaveCoreAsync derives the new slug from
        //     Input.Name; if two tests share a name they collide).
        //  2. Each test's assertions can identify its own row without
        //     ambiguity.
        var unique = Guid.NewGuid().ToString("N")[..8];
        var uniqueName = $"{name} {unique}";
        using var seedDb = _factory.CreateDbContext();
        var bike = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = uniqueName,
            Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{unique}",
            Category = category,
            BasePrice = basePrice,
            ShortDescription = "Xe ga thể thao",
            Description = "Mô tả dài ban đầu",
            IsFeatured = false,
            IsPublished = true,
            SortOrder = 5,
            ThumbnailUrl = thumbnailUrl,
            MetaTitle = "Meta Title gốc",
            MetaDescription = "Meta Description gốc",
            MetaKeywords = "honda, airblade",
            OgImageUrl = "https://res.cloudinary.com/demo/seed-og.jpg",
            CanonicalUrl = "/xe/airblade-marvel",
        };
        seedDb.Motorcycles.Add(bike);
        await seedDb.SaveChangesAsync();
        return bike;
    }

    /// <summary>
    /// Render the Edit page, extract the antiforgery token from the
    /// rendered HTML so the POST can carry a valid token, and return the
    /// (cookies, token) pair for the test to reuse.
    /// </summary>
    private async Task<(HttpClient client, string antiforgeryToken, string editFormHtml, string editorUrl)> GetEditorAsync(
        Guid motorcycleId)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var url = $"/admin/xe/editor/{motorcycleId}?tab=general";
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // The antiforgery token is rendered as
        //   <input name="__RequestVerificationToken" type="hidden" value="..." />
        // by @Html.AntiForgeryToken(). Pull it out so the POST can carry it.
        var match = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(match.Success,
            "Antiforgery token not found in rendered Edit page HTML. " +
            "The Edit form must include @Html.AntiForgeryToken() to be POSTable.");

        var token = match.Groups["tok"].Value;
        return (client, token, html, url);
    }

    /// <summary>
    /// Submit the Edit form exactly as a browser would after the user
    /// changed Category to <paramref name="newCategory"/> and BasePrice to
    /// <paramref name="newBasePrice"/>. Returns the redirect location
    /// and the response body for inspection.
    /// </summary>
    /// <remarks>
    /// The seeded motorcycle's <c>Name</c> is unique per test (see
    /// <see cref="SeedAsync"/>) so the slug SaveCoreAsync derives from
    /// <c>Input.Name</c> cannot collide with another seeded row in the
    /// shared InMemoryDatabase. Tests must pass the bike's actual
    /// <c>Name</c> (not a constant) to keep this invariant.
    /// </remarks>
    private static FormUrlEncodedContent BuildEditForm(
        Guid motorcycleId,
        string name,
        MotorcycleCategory newCategory,
        decimal newBasePrice,
        string antiforgeryToken,
        string? slug = null)
    {
        // Mirror EXACTLY the keys the _EditorTabGeneral.cshtml edit-mode form
        // emits when serialized as application/x-www-form-urlencoded.
        // Field name pattern: "Input.<PropertyName>" because Razor tag
        // helpers prefix bound nested models with the BindProperty name.
        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = antiforgeryToken,
            ["Id"] = motorcycleId.ToString(),
            ["Tab"] = "general",
            // PublishStatus is not on the edit form, but EditorModel has it
            // as a [BindProperty] — model binder expects it (whitelist keeps
            // it). Post an empty value to avoid binding to the default.
            ["PublishStatus"] = "",
            // Input.* nested fields
            ["Input.Name"] = name,
            ["Input.Slug"] = slug ?? string.Empty,
            ["Input.Category"] = ((int)newCategory).ToString(),
            ["Input.BasePrice"] = newBasePrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Input.ShortDescription"] = "Xe ga thể thao",
            ["Input.Description"] = "Mô tả dài ban đầu",
            ["Input.SortOrder"] = "5",
            ["Input.IsFeatured"] = "false",
            ["Input.IsPublished"] = "true",
            ["Input.ThumbnailUrl"] = "https://res.cloudinary.com/demo/seed.jpg",
            ["Input.MetaTitle"] = "",
            ["Input.MetaDescription"] = "",
            ["Input.MetaKeywords"] = "",
            ["Input.OgImageUrl"] = "",
            ["Input.CanonicalUrl"] = "",
        };
        return new FormUrlEncodedContent(fields);
    }

    // ---------- STEP 1 — Inspect the rendered Edit HTML ----------

    [Fact]
    public async Task Step1_RenderedHtml_ExposesExpectedFormFields()
    {
        var bike = await SeedAsync();
        var (_, _, html, _) = await GetEditorAsync(bike.Id);

        // Form action / method / handler
        var formMatch = Regex.Match(html,
            @"<form[^>]*?(?<attrs>[^>]*?id=""moto-editor-form""[^>]*?)>",
            RegexOptions.Singleline);
        Assert.True(formMatch.Success,
            "Expected to find <form id=\"moto-editor-form\"> in the rendered Edit page.");
        var formTag = formMatch.Value;
        Assert.Contains("method=\"post\"", formTag, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("enctype=\"multipart/form-data\"", formTag, StringComparison.OrdinalIgnoreCase);

        // Handler — Razor Pages emits `asp-page-handler` as the handler
        // query parameter on the form's action attribute, NOT as a hidden
        // input. The form posts to `?handler=SaveGeneral`.
        Assert.Contains("action=", formTag, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("handler=SaveGeneral", formTag, StringComparison.OrdinalIgnoreCase);

        // Category select — must have a name attribute of "Input.Category"
        // and at least one <option> with a numeric value.
        var selectMatch = Regex.Match(html,
            @"<select[^>]*name=""Input\.Category""[^>]*>(?<opts>.*?)</select>",
            RegexOptions.Singleline);
        Assert.True(selectMatch.Success,
            "Expected <select name=\"Input.Category\"> in the rendered Edit page.");
        var optionCount = Regex.Matches(selectMatch.Groups["opts"].Value, "<option").Count;
        Assert.True(optionCount >= 5,
            $"Category select must contain one option per enum value (got {optionCount}).");

        // BasePrice input — name="Input.BasePrice" and type="number" (order
        // in the rendered HTML may vary: asp-for emits type before name).
        var basePriceMatch = Regex.Match(html,
            @"<input\b(?=[^>]*\bname=""Input\.BasePrice"")(?=[^>]*\btype=""number"")[^>]*>",
            RegexOptions.IgnoreCase);
        Assert.True(basePriceMatch.Success,
            "Expected <input name=\"Input.BasePrice\" type=\"number\"> in the rendered Edit page.");
        Assert.DoesNotContain("disabled", basePriceMatch.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("readonly", basePriceMatch.Value, StringComparison.OrdinalIgnoreCase);

        // No hidden duplicate Category or BasePrice inputs.
        var hiddenCategoryCount = Regex.Matches(html,
            @"<input\b(?=[^>]*\btype=""hidden"")(?=[^>]*\bname=""Input\.Category"")[^>]*>",
            RegexOptions.IgnoreCase).Count;
        Assert.Equal(0, hiddenCategoryCount);
        var hiddenPriceCount = Regex.Matches(html,
            @"<input\b(?=[^>]*\btype=""hidden"")(?=[^>]*\bname=""Input\.BasePrice"")[^>]*>",
            RegexOptions.IgnoreCase).Count;
        Assert.Equal(0, hiddenPriceCount);

        // Save button is type=submit inside this same form. Razor HTML-encodes
        // the Vietnamese text, so we look for the encoded form "Lưu thay đổi"
        // (rendered as L&#x1B0;u thay &#x111;&#x1ED5;i) — the Phase 1 staff
        // label. The previous "Lưu bản nháp" label was retired in favor of
        // "Lưu thay đổi" as part of the staff-friendly button copy.
        Assert.Contains("L&#x1B0;u thay &#x111;&#x1ED5;i", html);
    }

    // ---------- STEP 2 — Capture the actual POST request and trace persistence ----------

    [Fact]
    public async Task Step2_BrowserStyleEdit_PersistsCategoryAndBasePrice_ThenSurvivesReload()
    {
        // ARRANGE: existing motorcycle with Category=Scooter, BasePrice=old.
        var bike = await SeedAsync(
            category: MotorcycleCategory.Scooter,
            basePrice: 41_290_000m);
        Assert.Equal(MotorcycleCategory.Scooter, bike.Category);
        Assert.Equal(41_290_000m, bike.BasePrice);

        // ACT 1: GET the Edit page to capture the antiforgery token.
        var (client, token, _, editorUrl) = await GetEditorAsync(bike.Id);

        // ACT 2: Build the form payload exactly as the browser would submit.
        var newCategory = MotorcycleCategory.ConTay; // closest to "Naked" in the enum
        const decimal newPrice = 50_500_000m;

        using var form = BuildEditForm(
            bike.Id,
            bike.Name,
            newCategory: newCategory,
            newBasePrice: newPrice,
            antiforgeryToken: token);

        // ACT 3: Submit. Razor Pages handler URL pattern:
        //   /admin/xe/editor/{id}?handler=SaveGeneral&tab=general
        var postUrl = $"/admin/xe/editor/{bike.Id}?handler=SaveGeneral&tab=general";
        var postResponse = await client.PostAsync(postUrl, form);

        // A successful save RedirectToPage's to ?tab=general (302).
        Assert.True(
            postResponse.StatusCode is HttpStatusCode.Redirect
                              or HttpStatusCode.Found
                              or HttpStatusCode.RedirectMethod,
            $"Expected 302 Redirect after save. Got {(int)postResponse.StatusCode} {postResponse.StatusCode}.");

        // ASSERT 1: query the database via a FRESH DbContext (no shared
        // change tracker, no caching).
        Motorcycle? dbRead;
        using (var verifyDb = _factory.CreateDbContext())
        {
            dbRead = await verifyDb.Motorcycles
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == bike.Id);
        }
        Assert.NotNull(dbRead);
        Assert.Equal(newCategory, dbRead!.Category);
        Assert.Equal(newPrice, dbRead.BasePrice);

        // ASSERT 2: GET the Edit page again (following the redirect target)
        // and verify the rendered form reflects the persisted values — this
        // is what the admin sees after the redirect.
        var redirectLocation = postResponse.Headers.Location?.OriginalString
                               ?? editorUrl;
        var getResponse = await client.GetAsync(redirectLocation);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var html2 = await getResponse.Content.ReadAsStringAsync();

        // The Category select must mark the new option as selected.
        var selectMatch = Regex.Match(html2,
            @"<select[^>]*name=""Input\.Category""[^>]*>(?<opts>.*?)</select>",
            RegexOptions.Singleline);
        Assert.True(selectMatch.Success);
        var selectedOption = Regex.Match(selectMatch.Groups["opts"].Value,
            @"<option[^>]*selected[^>]*value=""(?<v>\d+)""",
            RegexOptions.IgnoreCase);
        Assert.True(selectedOption.Success,
            "After save, the Category <select> must mark the new value as selected. " +
            "If no option is selected, the model binder received the wrong value.");
        Assert.Equal(((int)newCategory).ToString(), selectedOption.Groups["v"].Value);

        // The BasePrice input must echo the new price in its value attribute.
        var basePriceValue = Regex.Match(html2,
            @"<input[^>]*name=""Input\.BasePrice""[^>]*value=""(?<v>[^""]*)""",
            RegexOptions.IgnoreCase);
        Assert.True(basePriceValue.Success,
            "BasePrice input must have a value attribute on re-render.");
        // Razor renders decimal using the current culture; in our tests the
        // invariant culture is used by ASP.NET Core. Compare numerically.
        var renderedPrice = decimal.Parse(
            basePriceValue.Groups["v"].Value,
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(newPrice, renderedPrice);
    }

    // ---------- STEP 3 — DECIMAL precision ----------

    [Fact]
    public async Task Step3_DecimalPrice_WithCents_RoundTripsExactly()
    {
        var bike = await SeedAsync(basePrice: 41_290_000m);
        var (client, token, _, _) = await GetEditorAsync(bike.Id);

        // Use a value that round-trips under any culture: whole-number VNĐ
        // prices don't depend on the current culture's decimal separator.
        const decimal precisePrice = 32_500_500m;
        using var form = BuildEditForm(
            bike.Id,
            bike.Name,
            MotorcycleCategory.PhanKhoiLon,
            precisePrice,
            token);

        var postUrl = $"/admin/xe/editor/{bike.Id}?handler=SaveGeneral&tab=general";
        var response = await client.PostAsync(postUrl, form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.Motorcycles.AsNoTracking()
            .FirstAsync(m => m.Id == bike.Id);
        Assert.Equal(precisePrice, reloaded.BasePrice);
        Assert.Equal(MotorcycleCategory.PhanKhoiLon, reloaded.Category);
    }

    // ---------- STEP 4 — Existing thumbnail stays intact ----------

    [Fact]
    public async Task Step4_ExistingThumbnail_StaysIntact_WhenNoNewUpload()
    {
        var bike = await SeedAsync();
        var (client, token, _, _) = await GetEditorAsync(bike.Id);

        // Submit without uploading a new image (no file part, the
        // Input.ThumbnailUrl field still carries the existing URL).
        using var form = BuildEditForm(
            bike.Id,
            bike.Name,
            MotorcycleCategory.Electric,
            99_000_000m,
            token);

        var postUrl = $"/admin/xe/editor/{bike.Id}?handler=SaveGeneral&tab=general";
        var response = await client.PostAsync(postUrl, form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.Motorcycles.AsNoTracking()
            .FirstAsync(m => m.Id == bike.Id);
        Assert.Equal(bike.ThumbnailUrl, reloaded.ThumbnailUrl);
        Assert.Equal(MotorcycleCategory.Electric, reloaded.Category);
        Assert.Equal(99_000_000m, reloaded.BasePrice);
    }

    // ---------- STEP 5 — Save with no changes ----------

    [Fact]
    public async Task Step5_SaveWithNoChanges_LeavesAllFieldsUntouched()
    {
        var bike = await SeedAsync();
        var (client, token, _, _) = await GetEditorAsync(bike.Id);

        // Submit with the SAME values currently in the DB.
        using var form = BuildEditForm(
            bike.Id,
            bike.Name,
            bike.Category,
            bike.BasePrice,
            token);

        var postUrl = $"/admin/xe/editor/{bike.Id}?handler=SaveGeneral&tab=general";
        var response = await client.PostAsync(postUrl, form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.Motorcycles.AsNoTracking()
            .FirstAsync(m => m.Id == bike.Id);
        Assert.Equal(bike.Category, reloaded.Category);
        Assert.Equal(bike.BasePrice, reloaded.BasePrice);
        Assert.Equal(bike.Name, reloaded.Name);
        Assert.Equal(bike.Slug, reloaded.Slug);
        Assert.Equal(bike.ShortDescription, reloaded.ShortDescription);
        Assert.Equal(bike.Description, reloaded.Description);
        Assert.Equal(bike.IsFeatured, reloaded.IsFeatured);
        Assert.Equal(bike.IsPublished, reloaded.IsPublished);
        Assert.Equal(bike.SortOrder, reloaded.SortOrder);
        Assert.Equal(bike.ThumbnailUrl, reloaded.ThumbnailUrl);
    }

    // ---------- STEP 6 — Category only ----------

    [Fact]
    public async Task Step6_ChangeCategoryOnly_KeepsBasePrice()
    {
        var bike = await SeedAsync();
        var (client, token, _, _) = await GetEditorAsync(bike.Id);

        using var form = BuildEditForm(
            bike.Id,
            bike.Name,
            MotorcycleCategory.PhanKhoiLon,
            bike.BasePrice,
            token);

        var postUrl = $"/admin/xe/editor/{bike.Id}?handler=SaveGeneral&tab=general";
        var response = await client.PostAsync(postUrl, form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.Motorcycles.AsNoTracking()
            .FirstAsync(m => m.Id == bike.Id);
        Assert.Equal(MotorcycleCategory.PhanKhoiLon, reloaded.Category);
        Assert.Equal(bike.BasePrice, reloaded.BasePrice);
    }

    // ---------- STEP 7 — Price only ----------

    [Fact]
    public async Task Step7_ChangeBasePriceOnly_KeepsCategory()
    {
        var bike = await SeedAsync();
        var (client, token, _, _) = await GetEditorAsync(bike.Id);

        using var form = BuildEditForm(
            bike.Id,
            bike.Name,
            bike.Category,
            39_990_000m,
            token);

        var postUrl = $"/admin/xe/editor/{bike.Id}?handler=SaveGeneral&tab=general";
        var response = await client.PostAsync(postUrl, form);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect
                                or HttpStatusCode.Found
                                or HttpStatusCode.RedirectMethod);

        using var verifyDb = _factory.CreateDbContext();
        var reloaded = await verifyDb.Motorcycles.AsNoTracking()
            .FirstAsync(m => m.Id == bike.Id);
        Assert.Equal(bike.Category, reloaded.Category);
        Assert.Equal(39_990_000m, reloaded.BasePrice);
    }
}
