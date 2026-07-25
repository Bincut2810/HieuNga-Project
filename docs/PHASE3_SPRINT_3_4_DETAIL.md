# Sprint 3.4 — Product Experience V2 (Motorcycle Detail)

**Date:** 2026-07-25  
**Scope:** Public `/xe/{slug}` only. Homepage, Admin CMS, installment formula, DB schema untouched.

---

## 1. Audit summary

| Block | Before (Sprint 3.2) | Gap for V2 |
|-------|---------------------|------------|
| Hero | Purchase panel above fold | Needed larger photo emphasis, purchase points, compare entry, zoom |
| Gallery | Thumb strip only | No fullscreen / keyboard / swipe lightbox |
| Color | Round swatches in hero | Needed color cards, large preview section, clearer persist UX |
| 360 | Drag + fullscreen + hint | Missing play/pause, auto-rotate, reset, load % |
| Features | Thumb carousel | Needed alternating premium feature rows |
| Technology | Static zigzag | Needed accordion expandable sections |
| Specs | Flat accordion list | Needed grouped cards, sticky nav, print |
| Purchase | Soft closing CTA only | Needed dedicated trust/purchase callout grid |
| Related | `_MotorcycleCard` ×4 | Needed premium cards + better ranking fill |
| Sticky | Works, hide on hero | Needed safer spacing / clearer primary |
| JS | Detail-scoped (good) | Extended viewer; finance still separate |

**Already removed earlier:** orphan `_DetailGallery.cshtml`, global detail JS from layout.

---

## 2. Problems found

1. Gallery not a real viewing experience (no lightbox).
2. Color UX too minimal for CMS multi-color media.
3. Features/tech felt like admin dumps, not showroom storytelling.
4. Specs hard to scan; no group navigation / print.
5. Related pool could be thin when category sparse.
6. No purchase-trust section between product story and calculator.

---

## 3. Removed / replaced

- Hero-only tiny color dots as primary color UI → upgraded + dedicated color section
- Feature Alpine carousel UI → alternating CMS feature rows
- Flat tech zigzag → accordion with illustration
- Flat specs list → grouped `<details>` + sticky nav
- Related `_MotorcycleCard` on detail → `_DetailRelatedCard`

Finance calculator markup/logic **unchanged**.

---

## 4. New Detail architecture

```
Hero V2 (photo + price + highlights + CTAs + compare)
  → Gallery lightbox (fullscreen)
  → Color experience (cards + large preview)
  → 360 premium controls
  → Features (alternating cards)
  → Technology (accordion)
  → Specs (grouped + sticky nav + print)
  → Purchase experience (trust cards)
  → Installment calculator (unchanged)
  → Related (premium cards, scored)
  → Soft CTA
Sticky mobile bar (hide while hero visible)
```

---

## 5. Files modified

- `Pages/Xe/ChiTiet.cshtml` — Product Experience V2 markup
- `Pages/Shared/_DetailRelatedCard.cshtml` — **new**
- `wwwroot/js/detail-viewer.js` — gallery lightbox, 360 controls, tech/specs Alpine
- `wwwroot/css/site.css` — detail V2 styles
- `Application/Services/MotorcycleService.cs` — related ranking/fill (featured + similar price)
- `docs/PHASE3_SPRINT_3_4_DETAIL.md` — this file

---

## 6. Performance improvements

- Detail JS still page-scoped (not global layout)
- Lazy images below fold; hero `fetchpriority=high`
- 360 staged preload + IntersectionObserver for remaining frames
- Gallery preload first colors + first gallery URLs
- Aspect-ratio / width-height to limit CLS
- `prefers-reduced-motion` disables auto-rotate / lifts
- Sticky bar uses transform transitions (no layout thrash)

---

## 7–8. Build & tests

- **Build:** succeeded (`dotnet build`) — existing CS9107 warnings only  
- **Tests:** passed — 10/10 (`HieuNga.Tests`)
