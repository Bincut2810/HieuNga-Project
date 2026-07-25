# Sprint 3.8 — Simplify Motorcycle Listing UX

## Changes

Public `/xe` is category-only Honda-style browse.

### Removed
- Search textbox, price range, sort dropdown, featured-only toggle, reset/clear filters
- `OnGetFilterAsync` catalog handler
- PageModel binds: `Q`, `MinPrice`, `MaxPrice`, `FeaturedOnly`, `Sort`
- Filter fields on `MotorcycleFilterDto` / repo `SearchAsync` (query, price, featured, sort keys)

### Kept
- Category chips (all 5 types + Tất cả) with counts
- Pagination (preserves category)
- Breadcrumb (Trang chủ / Mua xe)
- Motorcycle cards (thumbnail, stock, price, installment, CTA)

### Improved
- Active category highlight + count badges
- HTMX swaps `#catalog-browse` (chips + grid) with `hx-push-url`
- Mobile horizontal scroll chips
- Default order: Featured → In stock → Newest → Name

### Untouched
Admin CMS, DB schema, routes, motorcycle detail page.
