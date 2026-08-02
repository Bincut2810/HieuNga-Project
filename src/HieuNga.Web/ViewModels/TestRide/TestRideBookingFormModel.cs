using HieuNga.Application.DTOs;
using HieuNga.Application.TestRide;

namespace HieuNga.Web.ViewModels.TestRide;

/// <summary>Shared public Test Ride form (standalone page + Liên hệ embed).</summary>
public sealed class TestRideBookingFormModel
{
    public TestRideViewModel Input { get; set; } = new();
    public IReadOnlyList<TestRideMotorcycleOption> MotorcycleOptions { get; set; } = [];
    public IReadOnlyList<BranchDto>? Branches { get; set; }
    public string MinDate { get; set; } = TestRideVietnamTime.Today.ToString("yyyy-MM-dd");
    public string FormAction { get; set; } = "/dat-lich-lai-thu";
    public string SubmitLabel { get; set; } = "Đặt lịch xem xe";
    public string Footnote { get; set; } = "Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.";
    public bool Compact { get; set; }
}
