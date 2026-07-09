using HieuNga.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HieuNga.Web.ViewComponents;

public class SiteSettingsViewComponent(ISiteSettingsService siteSettings) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await siteSettings.GetAsync();
        return View(settings);
    }
}
