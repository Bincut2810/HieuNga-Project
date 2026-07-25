# Phase 4 — Sprint 4.1 Visual Polish / Micro-interactions / Motion

**Date:** 2026-07-25  
**Scope:** Public site CSS + `polish.js` + light markup hooks only.  
**Out of scope:** CMS, database, business logic, routing, finance formulas, Admin UI.

---

## 1. Audit (public pages)

| Surface | Findings before 4.1 |
|---------|---------------------|
| Homepage | Mixed section padding; reveal delays ad-hoc; cards hover uneven vs listing |
| Listing `/xe` | Skeleton OK; empty state plain; cards lacked shared reveal/stagger |
| Detail `/xe/{slug}` | Strong V2 visuals; ghost buttons / sticky CTA used parallel heights |
| Service / Promo / News / Contact / Finance / Search | Shared `btn-primary` but heights/radius varied via utility overrides; forms used mixed `lead-input` vs bare inputs |
| Footer | Social/newsletter controls smaller radius than cards |
| Global | Duplicate reveal/shimmer risk after early 4.1 draft; inconsistent shadows (`0 4px…` vs token), radius `0.75–1.25rem` hard-coded, transition timings mixed |

**Inconsistencies targeted**

- Button / input height  
- Card radius & shadow  
- Hover elevation  
- Reveal / delay system  
- Focus ring  
- Loading shimmer duplication  

---

## 2. Design tokens (`:root`)

Unified in `wwwroot/css/site.css`:

| Token group | Examples |
|-------------|----------|
| Motion | `--motion-fast/base/slow/slower`, `--ease-premium/soft/out` |
| Spacing | `--space-*`, `--section-py/px`, `--container-max`, `--header-offset`, safe-area |
| Radius | `--radius-sm/md/btn/card/card-lg/pill` |
| Shadow | `--shadow-xs/card/card-hover/elevated/glow-red/focus` |
| Controls | `--control-h`, `--input-h`, `--focus-ring` |
| Type | `--text-*`, `--leading-*`, `--tracking-*` |

Legacy aliases kept where pages already reference brand colors (`--honda-red`, etc.).

---

## 3. Motion (no GSAP)

| Effect | Implementation |
|--------|----------------|
| fade-up / left / right / scale | `.reveal`, `.reveal-up/left/right/scale` |
| stagger | `[data-stagger] > .reveal` delays + `initStagger()` |
| parallax (light) | `[data-parallax]` + rAF in `polish.js` |
| image reveal / shimmer | `.img-reveal`, `.img-wrap` shimmer until `.is-loaded` |
| button ripple | `--rx/--ry` + `.is-rippling` on `.btn-primary` / `.btn-ripple` |
| hover elevation | cards + `.hover-elevate` |
| counter | `[data-counter]` IntersectionObserver |
| section / page reveal | existing `.reveal` + `.page-enter` on HTMX swap |

`prefers-reduced-motion: reduce` disables transforms, parallax, ripple flash, and counters snap to final value.

---

## 4. Cards / buttons / forms / loading

- **Cards:** shared premium hover (lift, soft shadow, inset border, image zoom); CTA reveal (touch always-on).  
- **Buttons:** Primary / Secondary / Ghost / Icon — shared height, radius, focus, disabled, loading spinner, pressed.  
- **Forms:** `.form-control` / `.lead-input` height, focus, error/success, disabled; checkbox/radio accent + focus.  
- **Loading:** skeleton shimmer, spinner, empty-state, catalog/blog opacity while HTMX loading — no CLS from reserved skeleton overlay.

---

## 5. Responsive & a11y

- Fold phones (`≤380px`), tablet section rhythm, landscape sticky demotion, ultra-wide container, safe-area padding.  
- Focus-visible rings, reduced motion, touch-press class, keyboard-friendly CTAs unchanged.

---

## 6. Removed / consolidated CSS

- Duplicate `.reveal` / second `@keyframes shimmer` / orphaned skeleton block from interrupted draft (cleaned; single sources remain).  
- Replaced ad-hoc empty listing markup with `.empty-state`.  
- Dead utilities not bulk-deleted from Tailwind CDN usage; design-system tokens now own public control chrome.

---

## 7. Files modified

| File | Change |
|------|--------|
| `wwwroot/css/site.css` | Tokens, buttons, cards, forms, motion, loading, responsive polish |
| `wwwroot/js/polish.js` | Stagger, parallax, counters, ripple, HTMX re-init |
| `Pages/Index.cshtml` | `data-stagger`, parallax hero, service/why hooks, branch counter |
| `Pages/Shared/_HomeFeaturedCard.cshtml` | `.reveal` |
| `Pages/Shared/_MotorcycleCard.cshtml` | `.reveal` + `.card-cta-reveal` |
| `Pages/Shared/_PromotionCard.cshtml` | `.reveal` |
| `Pages/Shared/_BlogCard.cshtml` | `.reveal` |
| `Pages/Xe/_CatalogGrid.cshtml` | `data-stagger`, empty-state |
| `docs/PHASE4_SPRINT_4_1_POLISH.md` | This report |

---

## 8. Performance impact

- **No new libraries** (no GSAP).  
- Observers are one-shot (unobserve on reveal/counter).  
- Parallax is light (`translate3d`, rAF throttled); skipped under reduced motion.  
- Ripple is CSS opacity only.  
- CSS payload grew modestly (~tokens + unified components); duplicate rules removed.

---

## 9. Build / tests

Run from repo root:

```bash
dotnet build
dotnet test
```

**Verified (2026-07-25):** `dotnet build` OK · `dotnet test` **10/10** passed.
