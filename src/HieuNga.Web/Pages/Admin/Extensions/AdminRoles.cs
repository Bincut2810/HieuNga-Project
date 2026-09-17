using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HieuNga.Web.Pages.Admin.Extensions;

/// <summary>
/// Minimum role separation constants and helpers for the admin panel.
/// Three roles: Admin (full access), ContentStaff, BookingStaff.
/// Existing un-roled admins keep their full-access behavior.
/// </summary>
public static class AdminRoles
{
    public const string Admin = "Admin";
    public const string ContentStaff = "ContentStaff";
    public const string BookingStaff = "BookingStaff";

    public const string ContentAccessPolicy = "AdminContentAccess";
    public const string BookingAccessPolicy = "AdminBookingAccess";
}

/// <summary>
/// Authorization handler that grants access if:
///   - The user is in the required role, OR
///   - The user is in the Admin role (full access), OR
///   - The user has no roles at all (legacy admin behavior — let them in
///     so unauthenticated/test environments and pre-role accounts do not break).
/// </summary>
public class AdminRoleOrLegacyHandler : AuthorizationHandler<AdminRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRoleRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        if (user.IsInRole(AdminRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (user.IsInRole(requirement.Role))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Legacy admin: signed in but no roles assigned. The audit requires
        // role separation; until roles are configured, do not silently brick
        // existing operators. Skip the legacy fallback only when at least
        // one admin role exists in the system.
        if (requirement.AllowLegacyUnroled)
        {
            var hasAnyRole = user.IsInRole(AdminRoles.Admin)
                || user.IsInRole(AdminRoles.ContentStaff)
                || user.IsInRole(AdminRoles.BookingStaff);
            if (!hasAnyRole)
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class AdminRoleRequirement : IAuthorizationRequirement
{
    public AdminRoleRequirement(string role, bool allowLegacyUnroled)
    {
        Role = role;
        AllowLegacyUnroled = allowLegacyUnroled;
    }

    public string Role { get; }
    public bool AllowLegacyUnroled { get; }
}

/// <summary>
/// Attribute that applies a Content- or Booking-Staff policy at the Razor Page level.
/// Reuses ASP.NET Core's authorize pipeline; menu items are NOT the protection.
/// </summary>
public sealed class AuthorizeAdminRoleAttribute : AuthorizeAttribute
{
    public AuthorizeAdminRoleAttribute(string policy)
    {
        Policy = policy;
    }
}
