using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.LienHe;

public class IndexModel(
    IBranchService branchService,
    IBookingService bookingService,
    IMotorcycleService motorcycleService) : PageModel
{
    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    public MotorcycleDetailDto? SelectedMotorcycle { get; private set; }
    public string Intent { get; private set; } = "mua-xe";
    public string LeadSource { get; private set; } = "contact";
    public string? ServiceSlug { get; private set; }
    public string PageHeading { get; private set; } = "Liên hệ tư vấn";
    public string PageSubheading { get; private set; } = "Showroom HEAD — tư vấn tận tâm, phản hồi nhanh";
    public string SubmitLabel { get; private set; } = "Gửi yêu cầu tư vấn";

    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public string Phone { get; set; } = "";
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Subject { get; set; }
    [BindProperty] public string? Message { get; set; }
    [BindProperty] public Guid? MotorcycleId { get; set; }
    [BindProperty] public Guid? BranchId { get; set; }
    [BindProperty] public string? IntentField { get; set; }
    [BindProperty] public string? SourceField { get; set; }
    [BindProperty] public string? XeSlugField { get; set; }
    [BindProperty] public string? ServiceField { get; set; }

    public bool Success { get; private set; }

    public async Task OnGetAsync(
        string? intent,
        string? xe,
        string? service,
        string? source,
        CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        Intent = NormalizeIntent(intent);
        LeadSource = string.IsNullOrWhiteSpace(source) ? InferSource() : source.Trim().ToLowerInvariant();
        ServiceSlug = service;
        await LoadMotorcycleAsync(xe, ct);
        ApplyIntentCopy();
        PrefillSubjectMessage();
        BranchId ??= Branches.FirstOrDefault(b => b.IsHeadOffice)?.Id ?? Branches.FirstOrDefault()?.Id;
        MotorcycleId = SelectedMotorcycle?.Id;

        this.SetSeo(null, $"{PageHeading} | Xe Máy Hiếu Nga", PageSubheading);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        Intent = NormalizeIntent(IntentField);
        LeadSource = string.IsNullOrWhiteSpace(SourceField) ? "contact" : SourceField.Trim().ToLowerInvariant();
        ServiceSlug = ServiceField;
        await LoadMotorcycleAsync(XeSlugField, ct);
        if (MotorcycleId is null) MotorcycleId = SelectedMotorcycle?.Id;
        ApplyIntentCopy();

        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(Phone))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập họ tên và số điện thoại.");
            return Page();
        }

        await bookingService.CreateConsultationAsync(
            new CreateConsultationDto(
                CustomerName,
                Phone,
                Email,
                Subject,
                Message,
                BranchId ?? Branches.FirstOrDefault()?.Id,
                MotorcycleId,
                LeadSource,
                Intent,
                SelectedMotorcycle?.Slug ?? XeSlugField,
                ServiceSlug),
            ct);

        Success = true;
        this.SetSeo(null, "Đã gửi yêu cầu | Xe Máy Hiếu Nga", "Cảm ơn bạn đã liên hệ Xe Máy Hiếu Nga.");
        return Page();
    }

    private async Task LoadMotorcycleAsync(string? slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return;
        SelectedMotorcycle = await motorcycleService.GetBySlugAsync(slug.Trim(), ct);
    }

    private void ApplyIntentCopy()
    {
        var bikeName = SelectedMotorcycle?.Name;
        switch (Intent)
        {
            case "tra-gop":
                PageHeading = bikeName is null ? "Tư vấn trả góp" : $"Tư vấn trả góp — {bikeName}";
                PageSubheading = "Để lại thông tin — tư vấn viên hỗ trợ hồ sơ trả góp";
                SubmitLabel = "Gửi yêu cầu trả góp";
                break;
            case "bao-duong":
                PageHeading = "Tư vấn bảo dưỡng";
                PageSubheading = string.IsNullOrWhiteSpace(ServiceSlug)
                    ? "Đặt lịch / hỏi thông tin dịch vụ HEAD"
                    : $"Quan tâm dịch vụ: {ServiceSlug}";
                SubmitLabel = "Gửi yêu cầu dịch vụ";
                break;
            case "khuyen-mai":
                PageHeading = "Nhận ưu đãi khuyến mãi";
                PageSubheading = "Đăng ký nhận tư vấn ưu đãi đang áp dụng";
                SubmitLabel = "Đăng ký nhận ưu đãi";
                break;
            case "lai-thu":
                PageHeading = bikeName is null ? "Đặt lịch xem xe" : $"Đặt lịch xem — {bikeName}";
                PageSubheading = "Hoặc dùng form đặt lịch xem xe chuyên dụng";
                SubmitLabel = "Gửi yêu cầu xem xe";
                break;
            default:
                PageHeading = bikeName is null ? "Tư vấn mua xe" : $"Tư vấn mua — {bikeName}";
                PageSubheading = "Showroom HEAD — tư vấn tận tâm, phản hồi nhanh";
                SubmitLabel = "Gửi yêu cầu tư vấn";
                Intent = "mua-xe";
                break;
        }
    }

    private void PrefillSubjectMessage()
    {
        if (!string.IsNullOrWhiteSpace(Subject)) return;
        var bike = SelectedMotorcycle?.Name;
        Subject = Intent switch
        {
            "tra-gop" => bike is null ? "Tư vấn trả góp" : $"Tư vấn trả góp {bike}",
            "bao-duong" => string.IsNullOrWhiteSpace(ServiceSlug) ? "Tư vấn bảo dưỡng" : $"Tư vấn dịch vụ {ServiceSlug}",
            "khuyen-mai" => "Đăng ký nhận khuyến mãi",
            "lai-thu" => bike is null ? "Đặt lịch xem xe" : $"Đặt lịch xem {bike}",
            _ => bike is null ? "Tư vấn mua xe" : $"Tư vấn mua {bike}"
        };

        if (string.IsNullOrWhiteSpace(Message) && bike is not null)
        {
            Message = Intent switch
            {
                "tra-gop" => $"Tôi quan tâm trả góp mẫu {bike}. Vui lòng tư vấn hồ sơ và khoản tháng.",
                "lai-thu" => $"Tôi muốn đặt lịch xem mẫu {bike} tại showroom.",
                _ => $"Tôi quan tâm mẫu {bike}. Vui lòng tư vấn thêm."
            };
        }
    }

    private static string NormalizeIntent(string? intent)
    {
        var v = (intent ?? "mua-xe").Trim().ToLowerInvariant();
        return v switch
        {
            "tragop" or "installment" or "finance" => "tra-gop",
            "service" or "maintenance" or "baoduong" => "bao-duong",
            "promo" or "promotion" or "uu-dai" => "khuyen-mai",
            "testride" or "test-ride" or "xem-xe" => "lai-thu",
            "buy" or "consult" or "tu-van" => "mua-xe",
            _ => v
        };
    }

    private string InferSource()
    {
        var referer = Request.Headers.Referer.ToString();
        if (string.IsNullOrWhiteSpace(referer)) return "contact";
        try
        {
            var path = new Uri(referer).AbsolutePath.ToLowerInvariant();
            if (path is "/" or "") return "homepage";
            if (path.StartsWith("/xe/")) return "detail";
            if (path.StartsWith("/xe")) return "listing";
            if (path.StartsWith("/khuyen-mai")) return "promotion";
            if (path.StartsWith("/bao-duong")) return "service";
            if (path.StartsWith("/tra-gop")) return "finance";
            if (path.StartsWith("/tin-tuc")) return "news";
            if (path.StartsWith("/so-sanh")) return "compare";
        }
        catch { /* ignore */ }
        return "contact";
    }
}
