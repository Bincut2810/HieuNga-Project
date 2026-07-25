# Sprint 3.3 — Homepage Flagship Redesign

**Date:** 2026-07-25  
**Scope:** Public homepage only. Admin CMS, installment formula, Motorcycle CMS editor untouched. No DB migration.

---

## 1. Audit (before → after)

| Area | Before | Flagship |
|------|--------|----------|
| Hero | Autoplay carousel; no progress/keyboard polish | Full-bleed CMS carousel + progress bar, swipe, keyboard, pause on hover/focus |
| Categories | Text count cards | Large photo cards + count + CTA; placeholder when empty |
| Featured | Solid cards | Premium elevate/zoom + stock/featured badges + compare + favorite placeholder |
| Why | 4 trust points | 7 icon advantages (warranty → delivery) |
| Finance | Rate + banks | Bank “logos” (initials), down-payment/monthly examples, calculator CTA |
| Services | Price shown | Visual cards, price hidden, Book CTA |
| Promotions | Grid of small cards | Horizontal CMS rail + countdown placeholder |
| News | Equal cards | Magazine: large feature + side cards + read time |
| Reviews | Static grid | Auto-rotate slider (avatar, stars, branch) |
| Branches | Text info card | Map embed/fallback + address/phone/hours + Google Maps CTA |
| Footer | 4 columns | Motorcycles / Services / Finance / Contact + social + newsletter placeholder |

---

## 2. Removed / replaced

- Previous thin category text cards markup (replaced in place)
- Static review grid & equal news grid (replaced)
- Service price display on homepage cards
- Obsolete trust-only “why” block (expanded)

No Admin pages deleted. `_PromotionCard` / `_BlogCard` remain for other routes.

---

## 3. New homepage architecture

```
Hero (CMS banners)
  → Quick actions
  → Category photo experience
  → Featured showcase
  → Why Hiếu Nga
  → Finance highlight
  → Services
  → Promotions rail (CMS)
  → Magazine news (CMS)
  → Reviews slider (CMS)
  → Branches + map
Footer (site-wide)
```

Data: `IHomepageService` / `HomepageDto` (banners, featured, promos, branches, reviews, category counts+thumbs, posts, banks, services).

---

## 4. Files modified

- `Pages/Index.cshtml` — full flagship markup
- `Pages/Shared/_HomeFeaturedCard.cshtml` — premium actions
- `Pages/Shared/_Footer.cshtml` — IA + social + newsletter placeholder
- `Pages/Shared/_Layout.cshtml` — `#compare-toast` host
- `wwwroot/js/homepage.js` — hero/progress/keyboard + promo rail + review slider
- `wwwroot/css/site.css` — flagship styles + reveal variants + a11y helpers
- `Application/DTOs/MotorcycleDto.cs` — optional `ImageUrl` on category count
- `Application/Services/HomepageService.cs` — thumbs + more promos/posts
- `Domain/Interfaces/IMotorcycleRepository.cs` — category thumbnails API
- `Infrastructure/Repositories/MotorcycleRepository.cs` — implementation

---

## 5. Performance

- First hero slide: `fetchpriority=high`, eager; others lazy
- Category / promo / news / featured images: `loading="lazy"` + fixed width/height / aspect ratios (CLS-safe)
- Progress bar via `transform: scaleX` (no layout thrash)
- No animation libraries; IntersectionObserver via existing `polish.js` `.reveal`
- Reveal variants: fade-up / left / right / scale (CSS only)

---

## 6–7. Build & tests

Run locally after this sprint: `dotnet build` · `dotnet test`
