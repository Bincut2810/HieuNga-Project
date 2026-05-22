using System.ComponentModel.DataAnnotations;
using HieuNga.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.Admin;

[AllowAnonymous]
public class DangNhapModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Admin/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
            return RedirectToPage("/Admin/Index");

        ErrorMessage = "Email hoặc mật khẩu không đúng.";
        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await signInManager.SignOutAsync();
        return RedirectToPage("/Admin/DangNhap");
    }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }
}
