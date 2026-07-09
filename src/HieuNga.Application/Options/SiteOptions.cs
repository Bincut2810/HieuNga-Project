namespace HieuNga.Application.Options;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string Name { get; set; } = "Honda Hiếu Nga Đà Nẵng";
    public string BaseUrl { get; set; } = "https://hondahieunga.vn";
    public string Phone { get; set; } = "0905 123 456";
    public string Hotline { get; set; } = "0905 123 456";
    public string ZaloUrl { get; set; } = "https://zalo.me/0905123456";
    public string DefaultSeoTitle { get; set; } = "Honda Hiếu Nga Đà Nẵng | Mua xe & dịch vụ HEAD";
    public string DefaultSeoDescription { get; set; } =
        "Đại lý Honda HEAD chính hãng tại Đà Nẵng. Xe máy Honda, trả góp 0%, lái thử miễn phí, bảo dưỡng chuyên nghiệp.";
}
