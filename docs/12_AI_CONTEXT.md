# 12 — AI Context (Onboarding for Future Agents)

> **Read this document before writing any code.**  
> This is the compressed “institutional memory” of the Xe Máy Hiếu Nga / HieuNga codebase.

## One-sentence summary

ASP.NET Core 8 **Razor Pages monolith** for a motorcycle dealership showroom + Admin CMS, PostgreSQL-backed, Docker/Render deployable — **not** a full DMS/ERP.

## Brand vs code names

| Layer | Name |
|-------|------|
| Customer-facing brand | **Xe Máy Hiếu Nga** |
| Solution / namespaces / DB project | `HieuNga.*` |
| Historical brand in old commits/docs | Honda Hiếu Nga (migrated in UI/seeds) |

**Never** rename projects/namespaces for branding. **Do** update visible UI strings carefully.

## Architecture in 30 seconds

```
Browser → HieuNga.Web (Razor + HTMX + Alpine)
        → Application services (public use cases)
        → Infrastructure (EF, Identity, Cloudinary)
        → PostgreSQL
Admin CMS PageModels often skip Application and talk to EF/repos directly.
```

## Business domain (what exists)

- Motorcycle catalog (variants, colors, media, reviews)
- Promotions, blog, banners, branches
- Service catalog + maintenance booking leads
- Installment **estimation** + bank/rate config + installment request inbox
- Contact / test-ride / consultation leads
- Site settings KV + SEO fields
- Admin cookie login

## Business domain (what does NOT exist)

Invoice, payment, warranty module, inventory ledger, email/SMS, Redis, message bus, JWT API, role permissions matrix, customer accounts.

If asked to “implement warranty/payment/inventory”, treat as **new module design**, not a missing file.

## Critical modules

| Module | Why critical |
|--------|----------------|
| `DbInitializer` / `ServiceFinanceSeed` | Startup migrate+seed; can mutate banks/branding |
| `FinanceConfigService` + `finance-calculator.js` | Detail loan estimates (see PHASE3_FINANCE_FINAL.md) |
| `BookingService` | Lead capture integrity |
| `IImageStorageService` | Staging upload persistence |
| `SiteSettingsPageFilter` | Global contact/SEO injection |
| Soft-delete filters | Catalog visibility |

## Request flow (public)

1. Middleware (forwarded headers, static, authn/z, SeoMiddleware)  
2. Razor Page handler  
3. Application service → repository/DbContext  
4. DTO → cshtml (+ HTMX partials)  
5. Alpine for client widgets  

## Request flow (admin)

1. Cookie auth challenge if anonymous  
2. PageModel validates input  
3. Often `IRepository`/`DbContext` write  
4. Flash message + redirect  

## Database flow

- All writes eventually through `HieuNgaDbContext`.
- Unit of work: `IUnitOfWork.SaveChangesAsync`.
- Migrations auto-apply on boot.
- Soft delete: set `IsDeleted`; remember some tables lack query filters.

## Common patterns

- Feature folders under `Pages/`
- Vietnamese public URLs
- `SetSeo` extension for meta
- `SlugHelper` for unique slugs
- Options pattern + env `__` keys
- Seed gated by environment flags
- Image URL stored as string on entities

## Things you must never break

1. **Admin auth folder convention** (`AuthorizeFolder("/Admin")`).  
2. **Startup migration** reliability (site won’t boot if migrate fails).  
3. **Public service pages must not show prices** (customer requirement).  
4. **Finance public banks** expected partners (HD Bank, MB Bank, JACCS) unless product changes.  
5. **Brand string** Xe Máy Hiếu Nga on customer surfaces.  
6. **Deploy env contract** (`ConnectionStrings__DefaultConnection`, `Site__*`, seed/image keys).  
7. **No secrets in git**.  
8. **Local Development still works** with Docker Postgres on 5433.  
9. Don’t introduce a second conflicting calculator formula without aligning UI copy (“estimate”).  
10. Don’t overwrite Admin-edited CMS content on every startup.

## Hidden dependencies

- Public CSS/JS depends on **CDN** Tailwind/Alpine/HTMX.
- Layout OG images depend on `Site__BaseUrl`.
- Compare feature depends on cookie `honda_compare`.
- Production uploads need Cloudinary or URL-only workflow.
- Render free tier cold starts — health check path `/health`.
- `SiteSettings` DB values can override code defaults after first seed.
- Content enricher can overwrite motorcycle demo fields if enabled.

## Current coding style

- net8.0, nullable enabled, implicit usings  
- Primary constructors  
- Async I/O  
- Records for many DTOs  
- Minimal comments; Vietnamese user-facing text  

## Common mistakes to avoid

| Mistake | Why it hurts |
|---------|--------------|
| Adding Controllers “because REST” without decision | Splits API surface |
| Putting EF in Application | Breaks layering |
| Showing `EstimatedPriceText` on public bao-duong | Violates customer rule |
| Hardcoding old bank list only in JS | Drift from DB |
| Seeding weak Production admin | Security incident |
| Assuming Staging environment name exists | Uses Production + env |
| Editing brand by renaming solution | Massive unnecessary churn |
| Storing uploads only locally on Render | Data loss on redeploy |
| Relying on empty test project as safety | False confidence |

## Future implementation strategy (recommended order)

1. Stabilize tests around booking + installment + seed idempotency.  
2. Unify Admin writes behind Application commands gradually (feature by feature).  
3. Add rate limiting/CAPTCHA on public forms.  
4. Vendor or build CSS (reduce CDN SPOF) if intranet/offline matters.  
5. Introduce roles only when customer needs multiple admin types.  
6. Any ERP-like module (invoice/inventory) → new bounded context + tables + explicit UX, not bolted onto Booking.

## How to explore safely

1. Read `01_SYSTEM_OVERVIEW.md` + this file.  
2. Find route in `04_API.md`.  
3. Open PageModel → follow service/repo.  
4. Check entity in `03_DATABASE.md`.  
5. Check config keys in `07_CONFIGURATION.md` / `ENVIRONMENT.md`.  
6. **Do not** “clean up” seed/auth/deploy while implementing unrelated features.

## Definition of done for agents

- Build Release succeeds.  
- No unrelated refactors.  
- Docs/env updated when contracts change.  
- Customer rules (brand, no public service prices, finance partners) preserved unless the task explicitly changes them.
