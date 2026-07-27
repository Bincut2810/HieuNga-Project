using HieuNga.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin.Banner;

public class IndexModel(IImageStorageService imageStorage) : PageModel
{
    public bool SupportsImageUpload { get; private set; }

    public void OnGet()
    {
        ViewData["Title"] = "Banner trang chủ";
        SupportsImageUpload = imageStorage.SupportsUpload;
    }
}
