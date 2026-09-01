namespace HieuNga.Application.Options;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string Name { get; set; } = "Xe Máy Hiếu Nga";
    public string BaseUrl { get; set; } = "https://hondahieunga.vn";
    public string Phone { get; set; } = "0236 384 9556";
    public string Hotline { get; set; } = "0236 384 9556";
    public string ZaloUrl { get; set; } = "https://zalo.me/118680124068083722";
    public string DefaultSeoTitle { get; set; } = "Xe Máy Hiếu Nga | Mua xe và dịch vụ xe máy";
    public string DefaultSeoDescription { get; set; } =
        "Mua xe máy, tư vấn trả góp và đặt lịch sửa chữa, bảo dưỡng tại Đà Nẵng.";
}
