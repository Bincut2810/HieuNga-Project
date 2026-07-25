# 06 — Authorization & Authentication

## Summary

| Aspect | Implementation |
|--------|----------------|
| Framework | ASP.NET Core Identity |
| User store | EF Core → PostgreSQL table `admins` |
| User type | `ApplicationUser : IdentityUser<Guid>` |
| Auth scheme | **Cookie** only |
| JWT | Package referenced in Infrastructure — **not wired** |
| Public site | Fully anonymous |
| Admin | Cookie auth required for `/Admin` folder |

## Authentication

### Login

- Path: `/admin/dang-nhap`
- `SignInManager<ApplicationUser>.PasswordSignInAsync`
- Remember-me supported via checkbox
- `lockoutOnFailure: false` in current handler

### Cookie options (`Program.cs`)

| Option | Value |
|--------|-------|
| LoginPath | `/admin/dang-nhap` |
| AccessDeniedPath | `/admin/dang-nhap` |
| ExpireTimeSpan | 8 hours |
| SlidingExpiration | true |
| HttpOnly | true |
| SameSite | Lax |
| SecurePolicy | Always (non-Development); SameAsRequest (Development) |

### Logout

POST handler `OnPostLogoutAsync` on login page → `SignOutAsync`.

### Password rules (Identity options)

- Require digit, lowercase, uppercase
- Minimum length 8
- Unique email required

Production admin seed additionally requires password length ≥ 12 when seeding.

## Authorization

### Conventions

```csharp
options.Conventions.AuthorizeFolder("/Admin");
options.Conventions.AllowAnonymousToPage("/Admin/DangNhap");
```

Login PageModel also has `[AllowAnonymous]`.

### Roles

- `IdentityRole<Guid>` registered and migrated (`admin_roles`).
- **No** `RequireRole`, role seeding, or role checks in PageModels.
- Effectively: **any authenticated admin user** can access all Admin pages.

### Policies / claims / permission system

**None custom.** No permission matrix, no resource-based authorization handlers.

### Filters

- `SiteSettingsPageFilter` — data loading, not auth.
- No custom `IAuthorizationFilter` for Admin.

## Identity user fields

Beyond Identity defaults:

- `FullName`
- `IsActive`
- `CreatedAt`
- `LastLoginAt`

`IsActive` is not consistently enforced as a gate in the login handler (verify before relying on it).

## Admin seed security

| Environment | Behavior |
|-------------|----------|
| Development | Can seed default admin from appsettings |
| Staging/Production | Only if `AdminSeedEnabled` (+ aliases) and strong password; skip if user exists |
| Logging | Logs email on success; **must not log password** (current code logs email only) |

## SEO / privacy side effect

`SeoMiddleware` adds `X-Robots-Tag: noindex, nofollow` for `/admin` paths (not auth, but related to Admin surface exposure).

## Security observations (audit only)

1. Single-role Admin → no separation of editor vs manager.
2. JWT package is dead dependency.
3. CSRF protected by antiforgery on POSTs (good).
4. Forwarded headers cleared KnownNetworks for reverse proxy (Render) — necessary, but trust proxy boundary carefully.
5. Public forms have no CAPTCHA / rate limiting.
