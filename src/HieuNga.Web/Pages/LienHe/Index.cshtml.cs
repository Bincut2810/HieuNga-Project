using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.TestRide;
using HieuNga.Web.Extensions;
using HieuNga.Web.ViewModels.TestRide;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.LienHe;

public class IndexModel(
    IBranchService branchService,
    IMotorcycleService motorcycleService,
    ITestRideService testRideService) : PageModel
{
    public const string ContactPageSource = "ContactPage";

    public IReadOnlyList<BranchDto> Branches { get; private set; } = [];
    public MotorcycleDetailDto? SelectedMotorcycle { get; private set; }
    public string Intent { get; private set; } = "mua-xe";
    public string LeadSource { get; private set; } = "contact";
    public string? ServiceSlug { get; private set; }
    public string PageHeading { get; private set; } = "Liên hệ tư vấn";
    public string PageSubheading { get; private set; } = "Showroom HEAD — tư vấn tận tâm, phản hồi nhanh";

    public TestRideBookingFormModel BookingForm { get; private set; } = new();

    public async Task OnGetAsync(
        string? intent,
        string? xe,
        string? service,
        string? source,
        CancellationToken ct)
    {
        Branches = await branchService.GetActiveAsync(ct);
        Intent = NormalizeIntent(intent);
        LeadSource = string.IsNullOrWhiteSpace(source) ? InferSource() : source.Trim();
        ServiceSlug = service;
        await LoadMotorcycleAsync(xe, ct);
        ApplyIntentCopy();

        var options = await testRideService.GetMotorcycleOptionsAsync(ct);
        var input = new TestRideViewModel
        {
            Source = ContactPageSource,
            MotorcycleId = SelectedMotorcycle?.Id,
            AppointmentDate = TestRideVietnamTime.Today,
            AppointmentTime = "",
            BranchId = Branches.FirstOrDefault(b => b.IsHeadOffice)?.Id ?? Branches.FirstOrDefault()?.Id
        };

        BookingForm = new TestRideBookingFormModel
        {
            Input = input,
            MotorcycleOptions = options,
            Branches = Branches,
            MinDate = TestRideVietnamTime.Today.ToString("yyyy-MM-dd"),
            FormAction = "/dat-lich-lai-thu",
            SubmitLabel = "Đặt lịch xem xe",
            Footnote = "Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.",
            Compact = true
        };

        this.SetSeo(null, $"{PageHeading} | Xe Máy Hiếu Nga", PageSubheading);
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
                PageSubheading = "Đặt lịch xem xe — tư vấn viên hỗ trợ hồ sơ trả góp tại showroom";
                break;
            case "bao-duong":
                PageHeading = "Liên hệ dịch vụ";
                PageSubheading = string.IsNullOrWhiteSpace(ServiceSlug)
                    ? "Đặt lịch bảo dưỡng hoặc đặt lịch xem xe tại showroom"
                    : $"Quan tâm dịch vụ: {ServiceSlug}";
                break;
            case "khuyen-mai":
                PageHeading = "Nhận ưu đãi khuyến mãi";
                PageSubheading = "Đặt lịch xem xe để nhận tư vấn ưu đãi đang áp dụng";
                break;
            case "lai-thu":
                PageHeading = bikeName is null ? "Đặt lịch xem xe" : $"Đặt lịch xem — {bikeName}";
                PageSubheading = "Chỉ mất khoảng 30 giây — showroom sẽ gọi xác nhận";
                break;
            default:
                PageHeading = bikeName is null ? "Liên hệ — đặt lịch xem xe" : $"Tư vấn mua — {bikeName}";
                PageSubheading = "Showroom HEAD — đặt lịch xem xe, phản hồi nhanh";
                Intent = "mua-xe";
                break;
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
