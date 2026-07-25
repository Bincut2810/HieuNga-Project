# Phase 4 — Service Experience Redesign

**Date:** 2026-07-25  
**Scope:** Public service experience + Admin service CMS fields.  
**Out of scope:** Motorcycle catalog, finance formulas, admin auth, lead/booking entity logic.

---

## 1. Audit (before)

| Area | Finding |
|------|---------|
| Public routes | Only `/bao-duong` + `/bao-duong/{slug}` — CRUD-like compact cards |
| `/dich-vu` | Did not exist (admin-only path prefix) |
| Homepage | Icon-only cards; ignored `IconKey`; hardcoded gear SVG |
| Detail | Short description + includes only; no hero/gallery/FAQ/process |
| CMS | Name/slug/price/includes/SEO — no thumbnail/hero/gallery/FAQ |
| Dead | `MaintenanceServiceCatalog.cs` (unused) |
| Seed | 14 granular demo SKUs |

---

## 2. Public experience

| Route | Role |
|-------|------|
| `/dich-vu` | Premium listing — photography cards |
| `/dich-vu/{slug}` | Flagship detail (hero, intro, benefits, when-to-use, process, gallery, FAQ, CTA, related) |
| `/bao-duong` | Booking-focused page (lead flow unchanged) |
| `/bao-duong/{slug}` | Permanent redirect → `/dich-vu/{slug}` |

**Six flagship services** (seeded / ensured):

1. Sửa chữa & thay thế phụ tùng  
2. Bảo hành & bảo dưỡng  
3. Dầu nhớt chính hãng  
4. Sửa chữa lưu động  
5. Bảo hiểm xe máy  
6. Tân trang & chăm sóc xe  

Homepage shows 6 experience cards + Book / See all CTAs.

---

## 3. DB changes (minimal migration)

Migration: `20260725141547_ServiceExperienceContentFields`

Added to `service_items`:

- `ThumbnailUrl`
- `HeroImageUrl`
- `GalleryJson`
- `FaqJson`
- `WhenToUseJson`
- `ProcessJson`

Reused: Name, Slug, ShortDescription, DetailDescription, IncludesJson (benefits), SEO fields, IsActive / IsFeatured.

Startup seed `HieuNgaServiceExperienceSeed.EnsureAsync`: upserts 6 flagships (empty fields only), deactivates known legacy demo slugs.

---

## 4. Files

### Added
- `Application/Catalog/HieuNgaServiceExperience.cs`
- `Infrastructure/Persistence/HieuNgaServiceExperienceSeed.cs`
- `Infrastructure/Persistence/Migrations/20260725141547_ServiceExperienceContentFields.*`
- `Web/Pages/DichVu/Index.cshtml(.cs)`
- `Web/Pages/DichVu/ChiTiet.cshtml(.cs)`
- `Web/Pages/Shared/_ServiceExperienceCard.cshtml`
- `docs/PHASE4_SERVICE_EXPERIENCE.md`

### Removed
- `Web/Pages/BaoDuong/MaintenanceServiceCatalog.cs`

### Modified (key)
- `Domain/Entities/ServiceItem.cs`
- DTOs / `IServiceCatalogService` / `ServiceCatalogService` / `ServiceItemJson`
- `HomepageService`, `Index.cshtml`, header/footer/mobile CTA
- `BaoDuong/Index` (booking-only UI), `BaoDuong/ChiTiet` (redirect)
- Admin `BangGiaModels` + `_ServiceItemForm`
- `site.css`, `polish.js`
- `DbInitializer`

---

## 5. Performance

- Lazy-loaded card/gallery images  
- Aspect-ratio placeholders via `.img-wrap` / CSS `aspect-ratio`  
- Hero `fetchpriority="high"`  
- Reduced-motion: disable card hover transforms  

---

## 6. Build / tests

**Verified (2026-07-25):** `dotnet build` OK · `dotnet test` **10/10** passed.