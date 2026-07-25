# Phase 3 — Sprint 3.2: Motorcycle Detail Conversion

**Date:** 2026-07-25  
**Scope:** Public `/xe/{slug}` only. Admin CMS untouched. Installment formula untouched. No DB redesign.

---

## 1. Audit summary

| Block | Before | Friction |
|-------|--------|----------|
| Hero | Image + weak CTAs; price card only | Purchase facts below fold; “Tính trả góp” was primary |
| Gallery | Unused as UI strip | CMS gallery not browsable |
| Color | Hero swatches **and** full duplicate section | Extra scroll; same preview twice |
| 360 | Basic drag + counter | No fullscreen, weak hint/loading |
| Features / Tech / Specs | OK | Specs far down (acceptable after purchase panel) |
| Finance | Calculator kept | Unchanged (correct) |
| CTA | Mixed priority; hardcoded `tel:` | Intent → contact, not test-ride booking |
| Related | Same category only | No availability / featured / price scoring |
| Sticky | Always visible on mobile | Competes with above-fold CTAs |

**Duplication removed:** dedicated color section (merged into hero).  
**Scroll reduced:** purchase grid + full CTA set in first viewport.

---

## 2. UX improvements

1. **Above-fold purchase panel:** price, availability badge, category, engine, transmission, fuel/consumption (from specs or fuel type), warranty (from specs or showroom fallback).
2. **Color:** clearer selected ring, instant preview, `sessionStorage` remember, gallery thumbs, mobile-friendly targets.
3. **360:** fullscreen, drag hint, frame counter, loading spinner, staged preload.
4. **CTAs:** Primary **Tư vấn mua xe**; secondary **Trả góp**, **Đặt lịch xem xe** (`/dat-lich-lai-thu?xeId=`), **Gọi ngay**, **Chat Zalo** (SiteSettings). Sticky appears **only after scrolling past hero**.
5. **Related:** same category, scored by available → featured → similar price.
6. **Micro UX:** hero skeleton, empty specs message, `width`/`height` on images, `touch-press`, tighter section spacing.
7. **Perf:** detail JS removed from global `_Layout`; loaded only on detail page; idempotent Alpine registration.

---

## 3. Files modified

- `src/HieuNga.Web/Pages/Xe/ChiTiet.cshtml`
- `src/HieuNga.Web/Pages/Xe/ChiTiet.cshtml.cs`
- `src/HieuNga.Web/Pages/Shared/_Layout.cshtml`
- `src/HieuNga.Web/wwwroot/js/detail-viewer.js`
- `src/HieuNga.Web/wwwroot/js/polish.js`
- `src/HieuNga.Web/wwwroot/css/site.css`
- `src/HieuNga.Application/Interfaces/IMotorcycleService.cs`
- `src/HieuNga.Application/Services/MotorcycleService.cs`
- `docs/PHASE3_SPRINT_3_2_DETAIL.md` (this file)
- `docs/PHASE3_PUBLIC_WEBSITE_AUDIT.md` (roadmap note)

---

## 4. Build

Succeeded (`dotnet build`) — existing CS9107 warnings only.

## 5. Tests

Passed — 1/1 (`HieuNga.Tests`).

---

## 6. Remaining work for Sprint 3.3

Suggested focus (**lead capture / intent wiring**):

1. Honor `intent` + `xe` on `/lien-he` (prefill form / context).
2. Show selected bike name on `/dat-lich-lai-thu` when `xeId` present.
3. Optional compare entry on detail (“+ So sánh”).
4. Homepage CMS wiring (banners / promos / reviews) if not taken as parallel track.
5. Unify `/tra-gop` with detail bank list (keep formula) — or defer to 3.4.
