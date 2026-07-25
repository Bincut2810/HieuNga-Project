# 04 — API / HTTP Surface

> This product is a **Razor Pages monolith**, not a REST Web API.  
> “Endpoints” below = page routes + named handlers + one minimal API.

Authentication legend:

- **Anon** — public
- **Auth** — ASP.NET Identity cookie (Admin folder)

---

## Minimal API

| Method | Route | Auth | Purpose | Input | Output | Services / DB |
|--------|-------|------|---------|-------|--------|---------------|
| GET | `/health` | Anon | Liveness + DB connectivity | — | JSON status (200/503) | `HieuNgaDbContext.CanConnectAsync` — no secrets |

---

## Public pages

### Home & catalog

| Method | Route | Auth | Purpose | Input | Output | Services | DB affected |
|--------|-------|------|---------|-------|--------|----------|-------------|
| GET | `/` | Anon | Homepage | — | HTML | `IHomepageService` | Read: banners, motorcycles, promotions, branches, reviews |
| GET | `/xe` | Anon | Catalog | query filters | HTML | `IMotorcycleService.SearchAsync` | Read motorcycles |
| GET | `/xe` | Anon | Catalog browse (category + page) | `category`, `PageNumber` | HTML or `_CatalogBrowse` partial when `HX-Target=catalog-browse` | Read |
| GET | `/xe/{slug}` | Anon | Detail + finance UI | slug | HTML | Motorcycle + FinanceConfig + Installment | Read |
| GET | `/xe/{slug}?handler=CalculateFinancing` | Anon | HTMX finance result | price/down/term/rate | Partial | `IInstallmentService.Calculate` | None (compute) |
| GET | `/so-sanh` | Anon | Compare up to 3 bikes | cookie | HTML | Motorcycle + `CompareSessionService` | Read |
| GET | `/so-sanh?handler=Add&id=` | Anon | Add to compare | Guid | Redirect/partial | Compare cookie | Cookie only |
| GET | `/so-sanh?handler=Remove&id=` | Anon | Remove from compare | Guid | Redirect/partial | Compare cookie | Cookie only |

### Leads & services

| Method | Route | Auth | Purpose | Input | Output | Services | DB affected |
|--------|-------|------|---------|-------|--------|----------|-------------|
| GET/POST | `/dat-lich-lai-thu` | Anon | Test-ride booking | CreateBooking fields | HTML / success | `IBookingService.CreateTestRideBookingAsync` | Insert `bookings` |
| GET/POST | `/bao-duong` | Anon | Service list + booking | maintenance form | HTML | ServiceCatalog + Booking | Insert `maintenance_bookings` |
| GET | `/bao-duong/{slug}` | Anon | Service detail | slug | HTML | `IServiceCatalogService` | Read services |
| GET | `/tra-gop` | Anon | Standalone calculator UI | — | HTML | — | — |
| POST | `/tra-gop?handler=Calculate` | Anon | HTMX calc | price/down/term | Partial | `IInstallmentService.Calculate` | None |
| GET/POST | `/lien-he` | Anon | Contact / consultation | consultation fields | HTML | `CreateConsultationAsync` | Insert `bookings` (Consultation) |

### Content

| Method | Route | Auth | Purpose | Services | DB |
|--------|-------|------|---------|----------|-----|
| GET | `/khuyen-mai` | Anon | Promo list | `IPromotionService` | Read |
| GET | `/khuyen-mai/{slug}` | Anon | Promo detail | same | Read |
| GET | `/tin-tuc` | Anon | Blog list | `IBlogService` | Read |
| GET | `/tin-tuc?handler=Filter` | Anon | HTMX blog grid | same | Read |
| GET | `/tin-tuc/{slug}` | Anon | Blog detail | same | Read |
| GET | `/Error` | Anon | Error page | — | — |

**DTO notes (public writes):**
- Test ride: `CreateBookingDto`
- Maintenance: `CreateMaintenanceBookingDto`
- Contact: `CreateConsultationDto` (stored as Booking with notes prefix)
- Installment request entity exists (`CreateInstallmentRequestDto` / `SubmitRequestAsync`) but primary UX is calculator + separate Admin inbox; verify page wiring before assuming every calc submits a lead.

---

## Admin pages (Auth required except login)

### Auth

| Method | Route | Auth | Purpose | Services | DB |
|--------|-------|------|---------|----------|-----|
| GET/POST | `/admin/dang-nhap` | Anon | Login | `SignInManager` | Read `admins` |
| POST | `/admin/dang-nhap?handler=Logout` | Auth | Logout | SignOut | — |
| GET | `/admin` | Auth | Dashboard | DbContext aggregates | Read |

### Motorcycles CMS

| Method | Route | Purpose | DB |
|--------|-------|---------|-----|
| GET/POST | `/admin/xe` | List + TogglePublish | `motorcycles` |
| GET/POST | `/admin/xe/them` | Create (+ optional image upload) | Insert motorcycle; Cloudinary/local |
| GET/POST | `/admin/xe/sua/{id}` | Update | Update motorcycle |
| GET/POST | `/admin/xe/xoa/{id}` | Soft delete confirm | Soft-delete |
| GET/POST | `/admin/xe/{id}/gia` | Variants CRUD | `motorcycle_variants` |
| GET/POST | `/admin/xe/{id}/noi-dung` | Highlights/specs/media URLs | motorcycle JSON + `media_assets` |

Admin motorcycle writes typically use `IRepository` + `HieuNgaDbContext` **directly** (not Application `IMotorcycleService`).

### Content CMS

| Area | Routes | Tables |
|------|--------|--------|
| Banner | `/admin/banner`, `/them`, `/sua/{id}` | `banners` |
| Branch | `/admin/chi-nhanh`, `/them`, `/sua/{id}` | `branches` |
| Promo | `/admin/khuyen-mai`, `/them`, `/sua/{id}` | `promotions` |
| News | `/admin/tin-tuc`, `/them`, `/sua/{id}` | `blog_posts` (+ categories) |

### Services & finance CMS

| Route | Purpose | Tables |
|-------|---------|--------|
| `/admin/dich-vu/danh-muc` | Service categories upsert/delete | `service_categories` |
| `/admin/dich-vu/bang-gia` (+ them/sua) | Service items (internal prices) | `service_items` |
| `/admin/tra-gop/ngan-hang` | Banks | `banks` |
| `/admin/tra-gop/lai-suat` | Rates | `finance_rates` |

### Customer leads inbox

| Route | Purpose | Tables |
|-------|---------|--------|
| `/admin/khach-hang/lich-hen` (+ `/{id}`) | Bookings (test ride / consultation) | `bookings` |
| `/admin/khach-hang/bao-duong` (+ `/{id}`) | Maintenance leads | `maintenance_bookings` |
| `/admin/khach-hang/tra-gop` (+ `/{id}`) | Installment requests | `installment_requests` |

Detail POSTs update status / AdminNotes.

### Settings

| Route | Purpose | Tables |
|-------|---------|--------|
| `/admin/cai-dat` | Site settings | `site_settings` via `ISiteSettingsService` |

---

## Cross-cutting HTTP behavior

| Concern | Behavior |
|---------|----------|
| Antiforgery | Enabled for Razor POSTs |
| HTMX | Partial swaps on catalog, blog, compare, finance |
| SEO robots | `/admin*` gets `X-Robots-Tag: noindex, nofollow` |
| Site settings | Injected every page via `SiteSettingsPageFilter` |
| Compression / HSTS / HTTPS redirect | Enabled (HSTS non-Development) |

## Controllers / gRPC / SignalR

**None.**
