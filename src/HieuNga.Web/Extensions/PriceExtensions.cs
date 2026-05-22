namespace HieuNga.Web.Extensions;

public static class PriceExtensions
{
    public static string ToVnd(this decimal price) =>
        string.Format(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", price);

    public static string ToEstimatedMonthly(this decimal price)
    {
        var monthly = Math.Round(price * 0.8m / 12, 0);
        return string.Format(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", monthly);
    }
}
