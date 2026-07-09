using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Pages.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace HieuNga.Web.Pages.Admin.CaiDat;

public class IndexModel(ISiteSettingsService siteSettings) : PageModel
{
    [BindProperty]
    public SettingsInput Input { get; set; } = new();

    public class SettingsInput
    {
        [Required] public string SiteName { get; set; } = string.Empty;
        [Required] public string Hotline { get; set; } = string.Empty;
        [Required] public string Phone { get; set; } = string.Empty;
        public string ZaloUrl { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Address { get; set; } = string.Empty;
        public string OpeningHours { get; set; } = string.Empty;
        public string DefaultMetaTitle { get; set; } = string.Empty;
        public string DefaultMetaDescription { get; set; } = string.Empty;
        public string? FacebookUrl { get; set; }
        public string? FooterText { get; set; }
        public string ServicePricingDisclaimer { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Cài đặt site";
        var s = await siteSettings.GetAsync(ct);
        Input = Map(s);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Cài đặt site";
        if (!ModelState.IsValid) return Page();

        await siteSettings.UpdateAsync(new SiteSettingsDto(
            Input.SiteName, Input.Hotline, Input.Phone, Input.ZaloUrl, Input.Email,
            Input.Address, Input.OpeningHours, Input.DefaultMetaTitle, Input.DefaultMetaDescription,
            Input.FacebookUrl, Input.FooterText, Input.ServicePricingDisclaimer), ct);

        this.SetSuccess("Đã lưu cài đặt.");
        return RedirectToPage();
    }

    private static SettingsInput Map(SiteSettingsDto s) => new()
    {
        SiteName = s.SiteName, Hotline = s.Hotline, Phone = s.Phone, ZaloUrl = s.ZaloUrl,
        Email = s.Email, Address = s.Address, OpeningHours = s.OpeningHours,
        DefaultMetaTitle = s.DefaultMetaTitle, DefaultMetaDescription = s.DefaultMetaDescription,
        FacebookUrl = s.FacebookUrl, FooterText = s.FooterText,
        ServicePricingDisclaimer = s.ServicePricingDisclaimer
    };
}
