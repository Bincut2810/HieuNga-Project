# 10 — Technical Debt (Observation Only)

> **Do not fix in this phase.** Items are risks and smells discovered during audit.

## Architecture smells

1. **Split write paths:** Public leads use Application services; Admin CMS often uses `DbContext`/`IRepository` directly in PageModels → duplicated validation/mapping rules and weaker consistency.
2. **Anemic / UI-driven domain:** Business rules live mostly in PageModels and seed helpers, not domain services.
3. **Clean Architecture leak:** Web references Infrastructure types (`HieuNgaDbContext`, Identity) widely — acceptable for small monolith, but increases coupling.
4. **Dual maintenance models:** `BookingType.Maintenance` vs `MaintenanceBooking` entity — conceptual overlap.
5. **Two installment calculators:** Server amortization vs client simplified formula — UX risk of mismatched numbers.

## Duplication

- Brand/site defaults duplicated across `BrandDefaults`, `SiteOptions`, `SiteSettingsService.Defaults`, appsettings, filters.
- Service catalog historically had static `MaintenanceServiceCatalog` alongside DB catalog (DB is source of truth for public pages).
- Admin form patterns repeated across content modules (acceptable CRUD duplication, but large PageModel files).

## Dead / unused code

- `Microsoft.AspNetCore.Authentication.JwtBearer` package referenced but unused.
- Identity **roles** infrastructure unused (no policies/roles seeded for authorization).
- Possibly unused ViewComponent path if filter always injects settings (verify before removal).

## Large / complex units

- `ServiceFinanceSeed.cs` — seed + bank sync + branding migration in one static class.
- `DbInitializer.cs` — migrate + multi-seed orchestration.
- Admin `ContentModels.cs` / `BangGiaModels.cs` / `TraGopModels.cs` / `KhachHangModels.cs` — multi-PageModel files.

## Soft-delete inconsistency

`IsDeleted` column everywhere, but **no global query filter** on Bookings, MaintenanceBookings, InstallmentRequests, Banners, SiteSettings → soft-deleted rows may still appear unless manually filtered.

## Audit gaps

No EF interceptor for `UpdatedAt`/`CreatedAt` on direct DbContext updates outside repository helpers.

## Security / product risks

- No CAPTCHA or rate limiting on public lead forms → spam risk.
- Single admin privilege level.
- Login does not clearly enforce `IsActive`.
- Public CDNs required for Tailwind/Alpine/HTMX.
- Container local uploads are ephemeral without Cloudinary.

## Performance risks

- No caching of site settings / active banks / service catalog (DB hit via filter every page).
- Homepage loads multiple independent queries without coordinated caching.
- Tailwind CDN (runtime) vs build-time CSS — FOUC/perf and dependency on third party.
- `SiteSettingsPageFilter` runs for every Razor page including Admin.

## Maintainability risks

- Minimal automated tests (`HieuNga.Tests` near-empty).
- Vietnamese route/page names mixed with English namespaces — fine if consistent, but onboarding cost.
- JSON-as-text fields without strong typing/schema versioning.
- Seed sync that mutates Production finance partners on startup — powerful; must remain idempotent and well-understood.

## Scalability limits

- Vertical scale only (single monolith + single Postgres).
- No queue for lead processing.
- No read replicas / CQRS.
- Admin and public share same process (noisy neighbor under load).

## Coupling score drivers

Web → Infrastructure direct usage; Application interfaces split between Application and Infrastructure implementations; frontend tightly coupled to HTML structure via HTMX selectors.

## What is already strong

- Clear project layering skeleton.
- EF migrations present and applied on startup.
- Deploy docs and health check exist.
- Soft delete + SEO fields thought through for catalog content.
- Feature folders for public pages are navigable.
