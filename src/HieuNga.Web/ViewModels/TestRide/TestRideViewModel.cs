using System.ComponentModel.DataAnnotations;
using HieuNga.Application.TestRide;

namespace HieuNga.Web.ViewModels.TestRide;

public sealed class TestRideViewModel
{
    [Display(Name = "Họ tên")]
    public string CustomerName { get; set; } = "";

    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = "";

    [Display(Name = "Xe muốn xem")]
    public Guid? MotorcycleId { get; set; }

    [Display(Name = "Ngày hẹn")]
    [DataType(DataType.Date)]
    public DateTime AppointmentDate { get; set; } = TestRideVietnamTime.Today;

    [Display(Name = "Giờ hẹn")]
    public string AppointmentTime { get; set; } = "";

    public string? Source { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}
