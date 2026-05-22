using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
    public int HttpStatusCode { get; private set; } = 500;
    public string Message { get; private set; } = "Đã xảy ra lỗi. Vui lòng thử lại sau.";

    public void OnGet(int? code)
    {
        HttpStatusCode = code ?? 500;
        Message = HttpStatusCode switch
        {
            404 => "Trang không tồn tại.",
            _ => Message
        };
    }
}
