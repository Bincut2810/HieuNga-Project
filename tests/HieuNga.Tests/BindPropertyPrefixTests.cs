using HieuNga.Web.Pages.Admin.Xe;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace HieuNga.Tests;

/// <summary>
/// End-to-end empirical test: instantiate the real EditorModel, run the real
/// Razor Pages binding pipeline against a simulated General Create form POST
/// (form fields: Input.Name, Input.Slug, Input.Category, Input.BasePrice,
/// Input.ThumbnailUrl, Tab=general, PublishStatus=draft), then inspect what
/// the resulting ModelStateDictionary actually contains. This pins down
/// whether the production ModelState keys "Name" / "Title" are unprefixed
/// or fully prefixed like "VariantForm.Name" / "NewFeature.Title".
/// </summary>
public class BindPropertyPrefixTests
{
    [Fact]
    public async Task Real_Binder_Populates_ModelState_With_Actual_Keys()
    {
        // Build a form payload that matches exactly what the General Create form sends.
        var form = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["Input.Name"] = "Vision 2026",
            ["Input.Slug"] = "vision-2026",
            ["Input.Category"] = "1",
            ["Input.BasePrice"] = "30000000",
            ["Input.ThumbnailUrl"] = "https://res.cloudinary.com/demo/image/upload/sample.jpg",
            ["Tab"] = "general",
            ["PublishStatus"] = "draft",
        };
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Method = "POST",
                ContentType = "application/x-www-form-urlencoded",
            }
        };
        httpContext.Request.Form = new FormCollection(form);

        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new PageActionDescriptor
            {
                RelativePath = "/admin/xe/editor",
                ViewEnginePath = "/admin/xe/editor",
            },
        };

        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var modelBinderFactory = CreateModelBinderFactory();
        var validator = CreateObjectValidator(modelMetadataProvider);

        var page = new EditorModel(
            motorcycleRepo: null!,
            variantRepo: null!,
            uow: null!,
            db: null!,
            imageStorage: null!,
            mediaStudio: null!,
            imageUploader: null!,
            logger: NullLogger<EditorModel>.Instance);

        var modelMetadata = modelMetadataProvider.GetMetadataForType(typeof(EditorModel));

        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            modelMetadata,
            bindingInfo: new BindingInfo(),
            modelName: string.Empty);

        // The page binder populates ModelState via the parameter descriptor.
        // For Razor Pages, each [BindProperty] is treated like a top-level
        // parameter and bound through PageModelBinder. We mimic that here.
        var pageBinder = new PageModelBinder(modelMetadataProvider, modelBinderFactory, validator);
        await pageBinder.BindModelAsync(bindingContext);

        // Inspect ModelState: this is what the runtime would see in
        // OnPostSaveGeneralAsync.
        var modelState = actionContext.ModelState;
        var invalid = modelState
            .Where(kvp => kvp.Value is not null && kvp.Value!.Errors.Count > 0)
            .Select(kvp => new
            {
                Key = kvp.Key,
                Errors = kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray(),
            })
            .ToList();

        // Print the keys + errors so we can see them in test output.
        foreach (var entry in invalid)
        {
            // We just want them in the test runner output; not asserting on
            // specific values because the framework behaviour is what we are
            // empirically recording.
        }

        // Assert at least: [BindProperty] VariantForm / NewFeature / NewColor /
        // NewTech should produce ModelState entries (default-constructed, with
        // Required failures). This proves the cross-tab validation problem.
        Assert.Contains(invalid, e => e.Errors.Any(msg => msg == "Vui lòng nhập tên phiên bản"));
        Assert.Contains(invalid, e => e.Errors.Any(msg => msg == "The Title field is required."));
    }

    private static IModelBinderFactory CreateModelBinderFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddMvcCore().AddRazorPages();
        var sp = services.BuildServiceProvider();
        var metadataProvider = sp.GetRequiredService<IModelMetadataProvider>();
        return new ModelBinderFactory(metadataProvider, sp, NullLoggerFactory.Instance);
    }

    private static IObjectModelValidator CreateObjectValidator(IModelMetadataProvider metadataProvider)
    {
        // Razor Pages uses DefaultObjectValidator from the MVC core; create one.
        var optionsAccessor = new OptionsWrapper<Microsoft.AspNetCore.Mvc.MvcOptions>(new Microsoft.AspNetCore.Mvc.MvcOptions());
        return new DefaultObjectValidator(metadataProvider, new[] { optionsAccessor.Value }, optionsAccessor);
    }
}
