using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace HieuNga.Web.Pages.Admin.Extensions;

/// <summary>
/// Razor Pages convention that attaches an AuthorizeFilter with a given policy
/// to a set of page paths. Used to enforce role separation without scattering
/// [Authorize] attributes across every cshtml file.
/// </summary>
public class AuthorizePageConvention : IPageApplicationModelConvention
{
    private readonly string[] _pagePaths;
    private readonly string _policy;

    public AuthorizePageConvention(string policy, params string[] pagePaths)
    {
        if (string.IsNullOrWhiteSpace(policy))
            throw new ArgumentException("Policy is required.", nameof(policy));
        if (pagePaths is null || pagePaths.Length == 0)
            throw new ArgumentException("At least one page path is required.", nameof(pagePaths));
        _policy = policy;
        _pagePaths = pagePaths;
    }

    public void Apply(PageApplicationModel model)
    {
        var relative = "/" + model.RelativePath.Replace('\\', '/');
        if (Array.IndexOf(_pagePaths, relative) < 0) return;
        model.Filters.Add(new AuthorizeFilter(_policy));
    }
}
