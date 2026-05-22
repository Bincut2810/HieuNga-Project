namespace HieuNga.Web.Services;

public class CompareSessionService(IHttpContextAccessor httpContextAccessor)
{
    private const string CookieName = "honda_compare";
    private const int MaxItems = 3;

    public IReadOnlyList<Guid> GetIds()
    {
        var cookie = httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(cookie)) return [];

        return cookie.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(MaxItems)
            .ToList();
    }

    public void Add(Guid id)
    {
        var ids = GetIds().ToList();
        if (!ids.Contains(id))
            ids.Insert(0, id);
        ids = ids.Take(MaxItems).ToList();
        SetCookie(ids);
    }

    public void Remove(Guid id)
    {
        var ids = GetIds().Where(x => x != id).ToList();
        SetCookie(ids);
    }

    public void Clear() => SetCookie([]);

    private void SetCookie(List<Guid> ids)
    {
        var response = httpContextAccessor.HttpContext?.Response;
        if (response is null) return;

        if (ids.Count == 0)
        {
            response.Cookies.Delete(CookieName);
            return;
        }

        response.Cookies.Append(CookieName, string.Join(',', ids), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(7),
            IsEssential = true
        });
    }
}
