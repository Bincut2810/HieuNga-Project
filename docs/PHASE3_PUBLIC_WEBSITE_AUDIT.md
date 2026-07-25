# Phase 3 — Public Website Audit & Sprint 3.1

**Date:** 2026-07-25  
**Scope:** Customer-facing site only. Admin CMS untouched. No DB redesign.

---

## 1. Public Website Audit

| Route | Page | Data source | Notes |
|-------|------|-------------|--------|
| `/` | Homepage | `IHomepageService` | Featured bikes + branches used; banners/promos/reviews loaded but unused |
| `/xe` | Listing / category | `IMotorcycleService` | CMS search; category chips + filters (Sprint 3.1) |
| `/xe/{slug}` | Detail | Motorcycle CMS + finance banks | Refactored; CTAs still hardcode some phone/intent gaps |
| `/bao-duong` | Service | Service catalog + booking | CMS-driven catalog |
| `/lien-he` | Contact | Branches + booking | Ignores `intent` / `xe` query params |
| `/tra-gop` | Finance | Installment service | Standalone calc; not bank CMS like detail |
| `/dat-lich-lai-thu` | Test ride | Booking | Weakly linked from primary UX |
| `/so-sanh` | Compare | Session + bikes | No add-to-compare entry on cards/detail |
| `/khuyen-mai` | Promotions | Promotions CMS | Footer only |
| `/tin-tuc` | Blog | Blog CMS | Orphaned from main nav |

### Duplicated layouts / unused / legacy

- **Hero patterns:** `_PageHero` vs custom dark bands (home, `/xe`, detail) — inconsistent.
- **Unused homepage CMS:** `HeroBanners`, `Promotions`, `Testimonials` fetched, not rendered.
- **Hardcoded:** `_CtaBanner` Unsplash + phone; some hotline fallbacks; home service tiles.
- **Dead / waste:** `_DetailGallery.cshtml` orphan; `detail-finance.js` + `detail-viewer.js` loaded globally; `/so-sanh` without entry points; admin link in public header.
- **Dual finance:** Detail (banks + prefs) vs `/tra-gop` (fixed default rate).

---

## 2. UX Problems (conversion-focused)

1. Listing previously lacked price range, sort, featured filter, and real pagination.
2. No stock/availability signal on cards (hardcoded “Trả góp 0%” instead).
3. Category discovery weak (select only; home chips OK but listing experience thin).
4. Featured section could disappear if no `IsFeatured` flags (repo now falls back).
5. Contact/test-ride intent params not honored → drop-off after detail CTA.
6. Compare & news/promos hard to find → secondary conversion paths unused.
7. Global detail JS + hardcoded CTAs hurt trust and performance.

---

## 3. Sprint roadmap

| Sprint | Focus | Deployable outcome |
|--------|--------|-------------------|
| **3.1** ✅ | Category chips + counts, listing filters (price/sort/featured), availability badges, featured reliability | Better browse → detail funnel |
| **3.2** ✅ | Detail conversion: above-fold purchase info, color/360, CTAs, related ranking, scoped JS | Detail → lead funnel |
| **3.3** ✅ | Demo Motorcycle Import System (metadata packages + Cloudinary upload + Admin UI) | Instant CMS demo data |
| **3.4** | Intent wiring on contact/test-ride; compare entry; optional home CMS | Lead capture |
| **3.5** | Unify finance UX (`/tra-gop` + detail banks); keep installment formula | Finance conversion |
| **3.6** | Nav IA + orphan routes; replace `_CtaBanner` hardcodes | Polish & cleanup |

---

## 4. Sprint 3.1 implementation

### Done

- **Category experience:** Chip strip on `/xe` with live published counts; home chips unchanged; “Tất cả” + active state.
- **Listing filters:** Search, category, min/max price, sort (`default` / `price_asc` / `price_desc` / `newest` / `name`), featured-only, HTMX + `hx-push-url`, pagination.
- **Availability badges:** From variant `IsAvailable` → “Còn hàng” / “Hết hàng” on `_MotorcycleCard` (replaces hardcoded “Trả góp 0%”).
- **Featured:** Repo fallback to latest published if none featured; listing `FeaturedOnly`; home CTA → `/xe?FeaturedOnly=true`.

### Constraints honored

- No Admin architecture changes  
- No DB redesign / migrations  
- No installment formula changes  
- Additive DTO/repo params only  

---

## 5. Files modified

- `src/HieuNga.Application/DTOs/MotorcycleDto.cs`
- `src/HieuNga.Application/Mappings/EntityMappers.cs`
- `src/HieuNga.Application/Interfaces/IMotorcycleService.cs`
- `src/HieuNga.Application/Services/MotorcycleService.cs`
- `src/HieuNga.Domain/Interfaces/IMotorcycleRepository.cs`
- `src/HieuNga.Infrastructure/Repositories/MotorcycleRepository.cs`
- `src/HieuNga.Web/Pages/Xe/Index.cshtml`
- `src/HieuNga.Web/Pages/Xe/Index.cshtml.cs`
- `src/HieuNga.Web/Pages/Xe/_CatalogGrid.cshtml`
- `src/HieuNga.Web/Pages/Shared/_MotorcycleCard.cshtml`
- `src/HieuNga.Web/Pages/Index.cshtml`
- `docs/PHASE3_PUBLIC_WEBSITE_AUDIT.md` (this file)

---

## 6–7. Build & Tests

- **Build:** succeeded (`dotnet build HieuNga.sln`) — existing CS9107 warnings only  
- **Tests:** passed — 1/1 (`HieuNga.Tests`)
