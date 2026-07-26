# Motorcycle Detail Refactor — Phase Report

> Brand: Xe Máy Hiếu Nga · Route: `/xe/{slug}`  
> **Finance subsystem closed:** [PHASE3_FINANCE_FINAL.md](./PHASE3_FINANCE_FINAL.md) (replaces `detail-finance.js` / Alpine notes below).  
> **360 viewer (2026-07):** Six fixed angles (`detailAngleViewer` + Media Studio `/angles/{key}`). Legacy 36-frame FrameIndex / gap / preload queue removed.

---

## Phase 1 — Audit

### 1. Audit
See `docs/DETAIL_PAGE_REFACTOR_AUDIT.md`.

### 2. Implementation Plan
Map current ChiTiet + gallery + finance → Honda-like section order; identify removable UI; plan entities for Features / Technology / 360 + category remapping.

### 3. Files modified
None (read-only).

### 4. Files removed
None.

### 5. Migration needed?
Planned: yes (categories + content tables).

### 6. Testing checklist
- [x] Inventory Detail Razor / partials / JS / CSS
- [x] Confirm no Detail ViewComponent
- [x] Isolate finance stack as do-not-touch

---

## Phase 2 — Delete obsolete UI

### 1. Audit
Removed old gallery partial, dead `detailGallery` Alpine helper, obsolete gallery/spec-card styles; kept finance + SEO + PageModel load path.

### 2. Implementation Plan
Delete only obsolete Detail UI after new page structure landed in same effort.

### 3. Files modified
- `Pages/Xe/ChiTiet.cshtml` (full rebuild)
- `wwwroot/js/polish.js` (removed `detailGallery`)
- `wwwroot/css/site.css` (gallery/spec dead CSS cleaned; hero media kept)

### 4. Files removed
- `Pages/Xe/Shared/_DetailGallery.cshtml`

### 5. Migration needed?
No (UI only).

### 6. Testing checklist
- [x] Detail page renders without `_DetailGallery`
- [x] Finance partial still present
- [x] No `detailGallery` references

---

## Phase 3 — New category system

### 1. Audit
Old enum: Scooter, Sport, Naked, Adventure, Cub, Electric, Other.  
New only: **Scooter · Xe số · Xe côn tay · Xe phân khối lớn · Xe điện**.

### 2. Implementation Plan
Update enum + labels helper; remap DB ints; seed; Admin/listing dropdowns; breadcrumbs/cards.

### 3. Files modified
- `Domain/Enums/MotorcycleCategory.cs`
- `Domain/MotorcycleCategoryLabels.cs`
- `Infrastructure/Persistence/DbInitializer.cs` (Winner X / CB150R → `ConTay`)
- `Pages/Xe/Index.cshtml`, Admin Them/Sua/Index, `_MotorcycleCard`, Detail breadcrumbs
- Migration SQL remapping

### 4. Files removed
None.

### 5. Migration needed?
**Yes** — `20260725102641_MotorcycleDetailContentAndCategories` remaps `motorcycles.Category` and creates content tables.

### 6. Testing checklist
- [x] Enum values 0–4 only
- [x] Listing filter uses `MotorcycleCategoryLabels.All`
- [x] Admin CRUD dropdowns use new labels
- [x] Seed uses Scooter / ConTay
- [ ] Apply migration on target DB (`dotnet ef database update` or app startup migrate)

---

## Phase 4 — New Detail page

### 1. Audit
Target section order implemented; calculator kept via existing partial.

### 2. Implementation Plan
Rebuild ChiTiet sections 1–9; Alpine color + 360 + features + specs accordion; shared color state for Hero ↔ Color.

### 3. Files modified
- `Pages/Xe/ChiTiet.cshtml`
- `wwwroot/js/detail-viewer.js` (new)
- `Pages/Shared/_Layout.cshtml` (script include)
- Application DTO / mappers / repository includes for Features, Technologies, SpinFrames

### 4. Files removed
- `_DetailGallery.cshtml` (Phase 2)

### 5. Migration needed?
Uses Phase 3 tables (features / technologies / spin_frames).

### 6. Testing checklist
- [ ] Hero: breadcrumb, name, price, CTAs
- [ ] Color swap updates Hero + Color section together
- [ ] 360 drag (mouse + touch) with ≥2 frames
- [ ] Feature showcase prev/next/thumbs
- [ ] Technology alternating layout
- [ ] Specs table; accordion on mobile
- [ ] `#financing` calculator still calculates correctly
- [ ] Related + contact CTA + sticky bar + JSON-LD

---

## Phase 5 — Admin improvement

### 1. Audit
`/admin/xe/{id}/noi-dung` handles Colors, Features, Tech, 360 multi-upload, Specs lines via `IImageStorageService`.

### 2. Implementation Plan
Upload-first media; soft-delete items; auto frame index for 360; keep Them/Sua for general fields + SEO.

### 3. Files modified
- `Pages/Admin/Xe/NoiDung.cshtml` + `.cs`
- Motorcycle form category options / thumbnail upload path (existing storage)

### 4. Files removed
None (raw URL gallery lines superseded by upload sections).

### 5. Migration needed?
Same as Phase 3.

### 6. Testing checklist
- [ ] Add color (name, hex, image)
- [ ] Add feature / technology with image
- [ ] Multi-upload 360 → sorted frames
- [ ] Clear 360
- [ ] Save specs lines
- [ ] Dev local storage / Prod Cloudinary still works

---

## Phase 6 — Performance

### 1. Audit
Hero uses `fetchpriority="high"` + reserved aspect boxes; below-fold images `loading="lazy"`; color/360/feature preload in `detail-viewer.js`; Intersection Observer for full 360 preload.

### 2. Implementation Plan
CLS via aspect-ratio containers; preload color + spin frames; lazy load secondary media. WebP depends on upload format (Admin accepts webp).

### 3. Files modified
- `ChiTiet.cshtml`, `detail-viewer.js`, `site.css` (hero media)

### 4. Files removed
None.

### 5. Migration needed?
No.

### 6. Testing checklist
- [ ] Hero LCP image prioritized
- [ ] No large layout jump on color change (aspect boxes)
- [ ] 360 frames preload when section enters viewport

---

## Phase 7 — Responsive

### 1. Audit
Grid breakpoints (sm/md/lg); specs accordion opens by default on `md+`; mobile sticky bar; touch 360.

### 2. Implementation Plan
Tailwind responsive grids; sticky CTA on `< lg`.

### 3. Files modified
Detail markup + existing `.detail-sticky-bar` CSS.

### 4. Files removed
None.

### 5. Migration needed?
No.

### 6. Testing checklist
- [ ] Desktop ≥1024
- [ ] Tablet ~768
- [ ] Mobile ~375 (sticky bar + accordion)

---

## Phase 8 — Do not break

### 1. Audit
Untouched at the time: auth cookie / Admin folder; SEO `SetSeo` + Product JSON-LD; Cloudinary/Local storage; Application installment service; EF migrations applied on startup pattern.  
**(Finance later rewritten — see PHASE3_FINANCE_FINAL.md.)**

### 2. Implementation Plan
Verify Release build + existing tests; spot-check finance partial wiring.

### 3. Files modified
None beyond prior phases.

### 4. Files removed
None.

### 5. Migration needed?
Apply `MotorcycleDetailContentAndCategories` on deploy.

### 6. Testing checklist
- [x] `dotnet build -c Release` succeeded (pre-final polish)
- [x] `dotnet test -c Release` — 1 passed
- [ ] Login Admin still works
- [ ] Calculator monthly payment matches prior formula
- [ ] SEO meta on Detail still set
- [ ] Image upload Dev vs Prod storage

---

## Summary — key paths

| Area | Path |
|------|------|
| Detail | `Pages/Xe/ChiTiet.cshtml` |
| Finance (current) | `_DetailFinanceCalculator.cshtml`, `finance-calculator.js` — see PHASE3_FINANCE_FINAL.md |
| Viewer JS | `wwwroot/js/detail-viewer.js` |
| Admin media | `Pages/Admin/Xe/NoiDung.cshtml(.cs)` |
| Migration | `…/Migrations/20260725102641_MotorcycleDetailContentAndCategories.cs` |
| Audit | `docs/DETAIL_PAGE_REFACTOR_AUDIT.md` |
