# Motorcycle Detail Page — Phase 1 Full Audit

> Read-only audit before refactor. No deletions performed in this phase.  
> Brand: Xe Máy Hiếu Nga · Route: `/xe/{slug}`

---

## 1. DETAIL PAGE STRUCTURE (current)

```
ChiTiet.cshtml (/xe/{slug})
├── finance boot JSON + Alpine store init (detail-finance.js)
├── Section 1 — Top purchase
│   ├── Breadcrumb
│   ├── Partial: Xe/Shared/_DetailGallery  (main + thumbs carousel)
│   ├── Category · stock label
│   ├── H1 name + short description
│   ├── Price card (Alpine price + monthly estimate)
│   ├── Key specs grid (first 6 TechnicalSpecs)
│   └── CTAs: consult / test-ride / #financing
├── Section 2 — Partial: Shared/_DetailFinanceCalculator  ★ KEEP
│   └── Uses Alpine store motorcycleDetail + banks from DB
├── Section 3 — Vehicle information (merged gray band)
│   ├── Overview + highlights (HighlightsJson)
│   ├── Specs table (TechnicalSpecsJson, take 8)
│   ├── Variants + Colors (color picker does NOT swap main image)
│   └── Static “Hỗ trợ tại Xe Máy Hiếu Nga” cards
├── Section 4 — Related (partial _MotorcycleCard × 2)
├── Section 5 — Contact CTA
├── Mobile sticky bar (.detail-sticky-bar)
└── JSON-LD Product (SEO)
```

**Missing vs target Honda-like UX:** dedicated Hero, color-driven main image, 360 viewer, feature showcase carousel, technology alternating blocks, mobile accordion specs.

---

## 2. Current files (Detail-related)

### Razor / PageModels

| File | Role | Keep? |
|------|------|-------|
| `Pages/Xe/ChiTiet.cshtml` | Detail layout | Rebuild |
| `Pages/Xe/ChiTiet.cshtml.cs` | Load DTO, finance, related, SEO | Keep logic; extend DTO usage |
| `Pages/Xe/Shared/_DetailGallery.cshtml` | Alpine gallery + thumbs | Replace (obsolete UI) |
| `Pages/Shared/_DetailFinanceCalculator.cshtml` | Installment UI | **KEEP** (style only) |
| `Pages/Shared/_DetailFinancingResult.cshtml` | HTMX calc partial | **KEEP** |
| `Pages/Shared/_MotorcycleCard.cshtml` | Related cards | Keep |
| `Pages/Xe/Index.cshtml` + `_CatalogGrid` | Listing filters by category | Update labels |
| `Pages/Admin/Xe/*` | CRUD / Gia / NoiDung | Simplify later |

### JS

| File | Role | Keep? |
|------|------|-------|
| `wwwroot/js/detail-finance.js` | Alpine installment store + formula | **KEEP logic exact** |
| `wwwroot/js/polish.js` | HTMX + `detailGallery()` Alpine component | Gallery fn removable after new UI; finance boot keep |

### CSS

| Selector / area in `site.css` | Role | Keep? |
|-------------------------------|------|-------|
| `.detail-page`, `.detail-sticky-bar` | Layout / mobile bar | Adapt |
| `.detail-gallery *` | Old gallery | Removable after replace |
| `.detail-finance-panel`, `.finance-*`, `.detail-range` | Calculator chrome | Keep / polish |
| `.detail-spec-card` | Spec hover cards | Mostly unused in current markup → dead |

### Domain / Application (not UI)

| Piece | Role |
|-------|------|
| `Motorcycle` + `HighlightsJson` / `TechnicalSpecsJson` | Features as strings only; specs as JSON |
| `MotorcycleColor` | Name, Hex, ImageUrl — ImageUrl unused by detail color UI |
| `MediaAsset` | Gallery URL lines via Admin NoiDung |
| `MotorcycleCategory` enum | Scooter, Sport, Naked, Adventure, Cub, Electric, Other |
| `MotorcycleDetailDto` | GalleryUrls, Highlights, Specs, Colors, Variants |
| `IMotorcycleService.GetBySlugAsync` | Mapping via EntityMappers |

### ViewComponents

| Component | Detail-related? |
|-----------|-----------------|
| `SiteSettings` ViewComponent | Global only — not Detail-specific |

No Detail-specific ViewComponent exists.

---

## 3. Dependencies (safe vs forbidden)

| Dependency | Touch in refactor? |
|------------|--------------------|
| Installment calculator JS + partials + `IInstallmentService.Calculate` | **No business logic change** |
| `IFinanceConfigService` banks | No |
| SEO `SetSeo` + JSON-LD | Keep / refresh product schema only |
| Auth / Admin folder auth | No |
| Cloudinary / `IImageStorageService` | Reuse for uploads |
| DbContext / migrations for new content | Yes (Features, Tech, 360, categories) |
| Repository interfaces | Extend only if new entities |

---

## 4. Unused / duplicate / dead

| Item | Finding |
|------|---------|
| Color `ImageUrl` | Stored but Detail color buttons only toggle local Alpine `selected` — **no image swap** |
| `_DetailGallery` thumbs + hero thumbnail | Duplicate of same URL list; hero also uses ThumbnailUrl |
| Specs shown twice | Key specs in purchase column + full list below |
| Highlights + Description | Overlap with “feature” story |
| `.detail-spec-card` CSS | Little/no use in ChiTiet |
| Static support cards | Hardcoded marketing, not CMS |
| `HEAD` badge in gallery | Legacy Honda HEAD visual |
| No 360 / Feature showcase / Technology sections | Not implemented |
| Admin NoiDung URL lines | Raw URL entry — conflicts with “upload only” goal |
| Category Sport/Naked/Adventure/Cub/Other | To be replaced by 5 new categories |

---

## 5. Legacy Honda-branded UI bits (Detail)

- Gallery badge text `HEAD`
- Support card “Bảo hành HEAD” / “Honda Việt Nam” (product wording — keep carefully; not brand “Honda Hiếu Nga”)
- CSS class prefix `honda-red` / `honda-dark` (design tokens — keep; not customer brand string)

---

## 6. What can safely be removed (Phase 2 candidates)

**UI only — after replacement ready or in same PR as new sections:**

1. `Xe/Shared/_DetailGallery.cshtml` (once Hero + Color + Feature cover imagery)
2. Inline old “Tổng quan / Thông số / Phiên bản & màu” merged section markup in `ChiTiet.cshtml`
3. Duplicate key-specs grid in purchase column (specs move to dedicated section)
4. Dead `.detail-gallery` / unused `.detail-spec-card` CSS after cutover
5. `detailGallery()` in `polish.js` if nothing else calls it

**Do NOT remove yet:**

- `_DetailFinanceCalculator.cshtml`, `_DetailFinancingResult.cshtml`, `detail-finance.js` logic
- SEO / PageModel load path
- Related + contact CTA (will restyle)
- Domain entities / Application services (Phase 2 rule)

---

## 7. Data gaps for target UX

| Target section | Current data | Gap |
|----------------|--------------|-----|
| Hero | Thumbnail + gallery | Need hero-friendly image; OK with Thumbnail |
| Color selector | Colors exist | Wire ImageUrl → main visual |
| 360 | None | Need frame entities + Admin multi-upload |
| Feature showcase | HighlightsJson strings only | Need title+description+image+order |
| Technology | None | Need title+description+image+order |
| Specs | TechnicalSpecsJson | OK; add accordion UI |
| Calculator | Full | Keep |
| Categories | Old enum | Migration + display names |

---

## 8. Phase 1 conclusion

Detail page is a **single large Razor file** + one gallery partial + finance partials + shared card. No ViewComponent carousel library. Color/feature/360 Honda-like UX requires **new content model + Admin uploads + rebuilt ChiTiet**. Installment stack is isolated and must remain.

**Next:** Phase 2 delete obsolete UI only after/with new structure; Phase 3 category migration; Phase 4–5 new page + Admin.
