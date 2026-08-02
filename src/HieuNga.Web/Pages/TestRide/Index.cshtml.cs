using HieuNga.Application.Interfaces;
using HieuNga.Application.TestRide;
using HieuNga.Web.ViewModels.TestRide;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HieuNga.Web.Pages.TestRide;

public class IndexModel(
    ITestRideService bookingService,
    IBranchService branchService,
    ILogger<IndexModel> logger) : PageModel
{
    public const string DuplicateMessage =
        "Bạn đã gửi lịch hẹn trước đó. Nhân viên sẽ sớm liên hệ với bạn.";

    [BindProperty]
    public TestRideViewModel Input { get; set; } = new();

    public TestRideBookingFormModel Form { get; private set; } = new();

    public bool Success { get; private set; }
    public bool IsDuplicate { get; private set; }
    public string SuccessMotorcycleUrl { get; private set; } = "/xe";
    public string SuccessMessage { get; private set; } = "";
    public string SuccessCustomerName { get; private set; } = "";
    public string SuccessMotorcycleName { get; private set; } = "";
    public string SuccessAppointmentDate { get; private set; } = "";
    public string SuccessAppointmentTime { get; private set; } = "";
    public string SuccessReceivedAt { get; private set; } = "";

    public async Task OnGetAsync([FromQuery] Guid? xeId, [FromQuery] string? source, CancellationToken ct)
    {
        await LoadFormAsync(xeId, source, ct);
    }

    public Task<IActionResult> OnPostAsync(CancellationToken ct) =>
        ProcessBookingAsync(jsonResponse: false, ct);

    public Task<IActionResult> OnPostBookAsync(CancellationToken ct) =>
        ProcessBookingAsync(jsonResponse: true, ct);

    private async Task<IActionResult> ProcessBookingAsync(bool jsonResponse, CancellationToken ct)
    {
        var request = new TestRideRequest(
            Input.CustomerName,
            Input.PhoneNumber,
            Input.MotorcycleId,
            Input.AppointmentDate,
            Input.AppointmentTime,
            Input.Source,
            Input.Notes,
            Input.BranchId);

        TestRideResponse result;
        try
        {
            result = await bookingService.CreateAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TestRide booking failed for {Phone}", Input.PhoneNumber);
            if (jsonResponse)
            {
                return new JsonResult(new TestRideResponse(
                    Guid.Empty,
                    Success: false,
                    IsDuplicate: false,
                    Message: "Hệ thống đang bận. Vui lòng thử lại sau hoặc gọi hotline.",
                    CustomerName: Input.CustomerName ?? "",
                    MotorcycleName: "",
                    AppointmentDate: "",
                    AppointmentTime: Input.AppointmentTime ?? "",
                    MotorcycleUrl: null,
                    Errors: new Dictionary<string, string[]>
                    {
                        [""] = ["Hệ thống đang bận. Vui lòng thử lại sau hoặc gọi hotline."]
                    }));
            }

            ModelState.AddModelError(string.Empty, "Hệ thống đang bận. Vui lòng thử lại sau hoặc gọi hotline.");
            await LoadFormAsync(Input.MotorcycleId, Input.Source, ct);
            return Page();
        }

        if (!result.Success)
        {
            if (jsonResponse)
                return new JsonResult(result);

            if (result.Errors is not null)
            {
                foreach (var (key, messages) in result.Errors)
                {
                    var field = string.IsNullOrEmpty(key) ? string.Empty : $"Input.{key}";
                    foreach (var msg in messages)
                        ModelState.AddModelError(field, msg);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }

            await LoadFormAsync(Input.MotorcycleId, Input.Source, ct);
            return Page();
        }

        if (jsonResponse)
            return new JsonResult(result);

        Success = true;
        IsDuplicate = result.IsDuplicate;
        SuccessMotorcycleUrl = result.MotorcycleUrl ?? "/xe";
        SuccessMessage = result.IsDuplicate ? DuplicateMessage : result.Message;
        SuccessCustomerName = result.CustomerName;
        SuccessMotorcycleName = result.MotorcycleName;
        SuccessAppointmentDate = result.AppointmentDate;
        SuccessAppointmentTime = result.AppointmentTime;
        var now = TestRideVietnamTime.Now;
        SuccessReceivedAt = $"Đã ghi nhận lúc {now:HH:mm} {now:dd/MM/yyyy}";
        ViewData["Title"] = "Đặt lịch thành công";
        ViewData["MetaTitle"] = "Đặt lịch thành công | Xe Máy Hiếu Nga";
        return Page();
    }

    private async Task LoadFormAsync(Guid? xeId, string? source, CancellationToken ct)
    {
        var options = await bookingService.GetMotorcycleOptionsAsync(ct);
        var branches = await branchService.GetActiveAsync(ct);
        if (xeId.HasValue)
            Input.MotorcycleId = xeId;
        if (!string.IsNullOrWhiteSpace(source))
            Input.Source = source.Trim();
        if (string.IsNullOrWhiteSpace(Input.AppointmentTime))
            Input.AppointmentTime = TestRideValidator.AllowedAppointmentTimes[0];
        if (Input.AppointmentDate == default)
            Input.AppointmentDate = TestRideVietnamTime.Today;
        Input.BranchId ??= branches.FirstOrDefault(b => b.IsHeadOffice)?.Id ?? branches.FirstOrDefault()?.Id;

        Form = new TestRideBookingFormModel
        {
            Input = Input,
            MotorcycleOptions = options,
            Branches = branches,
            MinDate = TestRideVietnamTime.Today.ToString("yyyy-MM-dd"),
            FormAction = "/dat-lich-lai-thu",
            SubmitLabel = "Đặt lịch xem xe",
            Compact = false
        };

        ViewData["Title"] = "Đặt lịch xem xe";
        ViewData["MetaTitle"] = "Đặt lịch xem xe | Xe Máy Hiếu Nga";
    }
}
