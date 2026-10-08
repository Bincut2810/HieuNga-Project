# Phase 1 — Admin Handover Hardening

> **Goal.** Make the Admin CMS simple and reliable enough for the Hiếu Nga
> head-office staff to use every day. Zero developer jargon in the staff UI.
> Backend persistence is unchanged for the audited Motorcycle CRUD.

---

## 1. Files changed

| File | Change |
| --- | --- |
| `src/HieuNga.Web/Pages/Admin/Xe/Editor.cshtml.cs` | Staff success message on save-general changed to `"Đã thêm xe thành công"` (create) / `"Đã cập nhật thông tin xe"` (update). Persisted BEFORE the toast is set, so a failed `SaveChangesAsync` never produces a success message. |
| `src/HieuNga.Web/Pages/Admin/Xe/Xoa.cshtml.cs` | Success message simplified to `"Đã xóa xe"`. Persistence (soft-delete + `SaveChangesAsync`) runs before the toast. |
| `src/HieuNga.Web/Pages/Admin/Xe/Xoa.cshtml` | Body text aligned with the spec wording; secondary "Hủy" link goes back to the listing rather than to the editor. |
| `src/HieuNga.Web/Pages/Admin/Xe/Index.cshtml` | Slug display removed (was a per-row technical leak). Media-meter removed from the staff list (it still exists on the editor checklist). Native `confirm()` replaced with the `data-confirm` modal so the listing delete flow uses the same modal as the rest of the admin. |
| `src/HieuNga.Web/Pages/Admin/TinTuc/Index.cshtml.cs` | Admin-only `Row` record (with `CategoryName`); `OnPostDeleteAsync` handler so the list supports a `[Sửa] [Xóa]` action pair. Persistence runs before the toast. |
| `src/HieuNga.Web/Pages/Admin/TinTuc/Index.cshtml` | Slug display removed; category badge added; `[Sửa]` + `[Xóa]` actions (delete uses the `data-confirm` modal). |
| `tests/HieuNga.Tests/MotorcycleEditHttpReproTests.cs` | `Step1` HTML-shape assertion updated to look for the current "Lưu thay đổi" button copy (previously the stale "Lưu bản nháp"). The test was already failing before the change because the staff button copy had been updated; the test is now in sync. |

### Files NOT changed (already in good shape)

- `src/HieuNga.Web/.../Admin/Shared/_AdminFlash.cshtml` — still the canonical
  success/error TempData → toast bridge. `AdminUi.SetSuccess/SetError` already
  keep the success/error keys mutually exclusive.
- `src/HieuNga.Web/.../Admin/Shared/_AdminLayout.cshtml` — already loads
  `admin-toast.js`; the toast widget lives at `wwwroot/js/admin-toast.js`.
- `src/HieuNga.Web/Pages/Admin/Xe/Editor.cshtml` — the SEO tab is already
  removed from the navigation and renders a "Cấu hình nâng cao" placeholder
  if the URL is hit directly. `EditorModelStateIsolation` + `NormalizeTab`
  collapse any `?tab=seo` request to `?tab=general`.
- `src/HieuNga.Web/Pages/Admin/Shared/_BlogPostForm.cshtml` — already hides
  Slug, MetaTitle, MetaDescription, MetaKeywords, OgImageUrl, CanonicalUrl
  from the staff form. The backend `BlogPostInputModel` still binds them
  for the public site.
- `src/HieuNga.Web/.../Admin/Xe/MotorcycleInputModel.cs` and
  `ContentModels.cs` (BlogPostInputModel) — the SEO fields are kept in the
  data model so the public site can still read them; they are simply not
  rendered to staff.

---

## 2. UI simplifications

| Module | Before | After |
| --- | --- | --- |
| Motorcycle listing | Showed name + slug + price + category + status + media meter + updated date. Native browser `confirm()` for hide/show. | Shows name + price + category + status + updated date. No slug, no media meter (it still lives on the editor checklist). Hide/show uses the styled `data-confirm` modal. |
| Motorcycle delete | Navigated to `/admin/xe/xoa/{id}` page. | The confirmation page is still there as a safety net for direct URL access. The body now matches the spec wording exactly. |
| Motorcycle editor save bar | "Lưu bản nháp" (technical "draft" wording). | "Lưu thay đổi" (staff-friendly wording). |
| Motorcycle editor success | Create: long "Đã tạo Draft. Tiếp tục thêm ảnh đại diện, màu và góc xem." Update: "Đã lưu thay đổi." | Create: "Đã thêm xe thành công". Update: "Đã cập nhật thông tin xe". |
| Blog listing | Showed title + status + date + slug. Only had a `[Sửa]` action. | Shows thumb + title + status + category badge + publish date. `[Sửa]` + `[Xóa]` actions; delete uses the `data-confirm` modal. |
| Blog edit page | SEO fields hidden from form. | Already aligned — no change needed. |
| Toast system | `admin-toast.js` widget. | Confirmed working for `AdminSuccess` / `AdminError` TempData keys, with the `data-admin-flash` / `data-toast` trigger payload. The toast is auto-dismissed after 4.5 s (success) / sticky (error). |

---

## 3. Fields hidden from staff

The following SEO/technical fields are still present in the data model and
the public site reads them. They are simply **not rendered** in any staff
form, button, badge, or list row:

| Module | Hidden staff fields |
| --- | --- |
| Motorcycle | `Slug`, `MetaTitle`, `MetaDescription`, `MetaKeywords`, `OgImageUrl`, `CanonicalUrl` (the entire SEO tab and the per-row slug display). |
| Blog | `Slug`, `MetaTitle`, `MetaDescription`, `MetaKeywords`, `OgImageUrl`, `CanonicalUrl` (form inputs) and the per-row slug display. |
| Promotion | `_SeoFields.cshtml` partial is still mounted on `_PromotionForm.cshtml` (out of scope for this phase; promotion module is unchanged). |

---

## 4. Toast implementation

The toast is a vanilla-DOM widget at `wwwroot/js/admin-toast.js`. It is
loaded by `_AdminLayout.cshtml` after every page render. Wiring:

- `_AdminFlash.cshtml` reads the `AdminSuccess` / `AdminError` TempData
  keys (via the indexer so the entry is marked for deletion), and emits:
  - a hidden `<input data-toast data-toast-type="success|error" value="…">`
    (consumed by `wireFlashBanners`),
  - and a `<div data-admin-flash>` fallback banner that is hidden once the
    toast has fired (so no duplicate message is visible).
- `admin-toast.js` `wireFlashBanners()` scans both trigger shapes and
  dispatches a real toast (success = 4.5 s, error = sticky, info = 5 s).
- The toast has a manual close button (`&times;`).
- It floats bottom-right on desktop, top on mobile, with a max-width that
  respects the viewport (CSS in `admin.css`).
- No jQuery, no CDN.

The contract guarantees that `SetSuccess(...)` is only ever called after
`SaveChangesAsync(...)` returned without throwing:

- `EditorModel.SaveCoreAsync` (create + update): success written after
  `SaveChangesAsync` returns. Failures go through `try/catch` → `SetError`.
- `EditorModel.OnPostSavePublishAsync`, `OnPostSaveSpecsAsync`,
  `OnPostSaveVariantAsync`, `OnPostDeleteVariantAsync`,
  `OnPostAddFeatureAsync`, `OnPostUpdateFeatureAsync`,
  `OnPostDeleteFeatureAsync`, `OnPostAddTechAsync`,
  `OnPostUpdateTechAsync`, `OnPostDeleteTechAsync`,
  `OnPostDuplicateMotorcycleAsync`: same pattern.
- `XoaModel.OnPostAsync`: soft-delete + `SaveChangesAsync` then
  `SetSuccess("Đã xóa xe")`.
- `TinTuc.ThemModel.OnPostAsync`, `TinTucSuaModel.OnPostAsync`,
  `TinTucSuaModel.OnPostDeleteAsync`, `TinTuc.IndexModel.OnPostDeleteAsync`:
  same pattern.
- `AdminUi.SetSuccess/SetError` cross-clear the other key so a stale
  error never co-renders with a fresh success.

---

## 5. Build result

```
dotnet build HieuNga.sln --nologo -v minimal
  …
  Build succeeded.
  0 Error(s)
  8 Warning(s) — all pre-existing, unrelated to this phase:
    - CS9113 / CS9107: unused-parameter / captured-context warnings in the
      Infrastructure layer (pre-existing).
    - CS8602 in _ImageUploader.cshtml (pre-existing nullable-ref warning).
    - CS9113 'mediaStudio' unused parameter in EditorModel (pre-existing
      constructor — kept for future Media Studio call-sites).
```

## 6. Test result

```
dotnet test tests/HieuNga.Tests/HieuNga.Tests.csproj --nologo
  Passed!  - Failed: 0, Passed: 132, Skipped: 0, Total: 132
```

Pre-existing test coverage that pins the staff-friendly changes:

- `AdminFlashTests` — success/error keys are consumed exactly once,
  `SetSuccess` drops a pending error from a prior failed request, and vice
  versa.
- `EditorModelStateIsolationTests`, `EditorModelStateKeysTests` — ModelState
  pollution from the editor's nested `[BindProperty]` is stripped via the
  whitelist so the staff button text and field copy do not affect validation.
- `MotorcycleEditRegressionTests` — every audited field (Name, Category,
  BasePrice, Description, ShortDescription, SortOrder, IsFeatured,
  IsPublished, thumbnail) still persists after a General save, and the SEO
  fields are **not** wiped by a General save.
- `MotorcycleEditHttpReproTests` — the full HTTP round-trip (GET → render →
  POST → DB read) still persists Category + BasePrice, and the editor HTML
  still exposes the staff `Lưu thay đổi` button copy.
- `EditorHandlerTests` — SaveGeneral / SaveSpecs / SaveVariant /
  DeleteVariant all still work.
- `AngleImagesMappingTests`, `TestRidePhoneNormalizerTests`,
  `MediaStudioHelpersTests`, `BannerHeroDtoTests`,
  `FinanceCalculatorTests`, `UnitTest1` — unchanged from before this phase.

One test had to be updated to keep the suite green:

- `MotorcycleEditHttpReproTests.Step1_RenderedHtml_ExposesExpectedFormFields`
  was asserting on the old "Lưu bản nháp" button text. The button copy was
  already "Lưu thay đổi" (the staff-friendly label) before this phase, so
  the test was stale. Updated the assertion to look for the HTML-encoded
  "Lưu thay đổi" string (`L&#x1B0;u thay &#x111;&#x1ED5;i`).

## 7. Tests that could not be run

- **Live HTTP integration test against PostgreSQL** — the in-process tests
  use `Microsoft.EntityFrameworkCore.InMemory` because Render / GitHub CI
  has no PostgreSQL instance available. The InMemory provider does not
  exercise Npgsql-specific behaviour (advisory locks, JSON operators,
  trigger semantics). For full production confidence, run the
  `MotorcycleEditHttpReproTests` against a PostgreSQL test instance.

## 8. Persistence logic — what changed, what didn't

| Area | Status |
| --- | --- |
| `Motorcycle` create / update | Persistence logic UNCHANGED. Only the user-facing success string changed. |
| `Motorcycle` soft-delete (Xoa page) | Persistence logic UNCHANGED. Only the user-facing success string changed. |
| `Motorcycle` publish toggle (list page) | Persistence logic UNCHANGED. The native `confirm()` was swapped for the `data-confirm` modal; the handler is identical. |
| `MotorcycleVariant` add / update / delete | UNCHANGED. |
| `MotorcycleFeature` / `MotorcycleTechnology` add / update / delete / duplicate / reorder | UNCHANGED. |
| `Motorcycle` specs save | UNCHANGED. |
| `BlogPost` create / update (Them, Sua) | UNCHANGED. |
| `BlogPost` delete (from edit page) | UNCHANGED. |
| `BlogPost` delete (new — from the list page) | Mirrors the edit-page delete contract (`IsDeleted = true`, `UpdatedAt = now`, `SaveChangesAsync`, then toast). The handler is in `TinTuc.IndexModel.OnPostDeleteAsync`. |
| `Promotion`, `Branch`, `Banner` CRUD | UNCHANGED (out of scope for this phase). |

No database migration was created or applied. No field was removed from any
data model. The public website reads the same columns it always has.

## 9. Migrations

None. No schema changes. No EF Core migration file added.

## 10. Manual test matrix

Run through the list below before the staff handover. Each step must
produce the success message shown and the toast must appear bottom-right
within one second.

### Motorcycle (admin)

| # | Action | Expected success message | DB persistence check | Re-render check | Toast check |
| - | --- | --- | --- | --- | --- |
| 1 | Add a new motorcycle (General tab) | "Đã thêm xe thành công" | row appears in `motorcycles`, `is_deleted = false` | editor opens on the Media tab for the new id | ✓ |
| 2 | Change the name | "Đã cập nhật thông tin xe" | `name` column reflects the new value | input echoes the new value | ✓ |
| 3 | Change the category | "Đã cập nhật thông tin xe" | `category` column reflects the new enum | dropdown shows the new option selected | ✓ |
| 4 | Change the price (BasePrice) | "Đã cập nhật thông tin xe" | `base_price` column reflects the new decimal | input echoes the new value | ✓ |
| 5 | Change the description (long) | "Đã cập nhật thông tin xe" | `description` column reflects the new text | textarea echoes the new value | ✓ |
| 6 | Upload a new thumbnail | "Đã cập nhật thông tin xe" | `thumbnail_url` column reflects the uploaded URL | preview reloads | ✓ |
| 7 | Toggle publish (list page "Ẩn" / "Hiện") | "Đã hiển thị xe trên website." / "Đã ẩn xe khỏi website." | `is_published` column flips | badge flips | ✓ |
| 8 | Add a new color (Media Studio) | (existing message) | new row in `motorcycle_colors` | color list updates | ✓ |
| 9 | Edit a color (Media Studio) | (existing message) | `name` / `image_url` updated | color list updates | ✓ |
| 10 | Delete a color (Media Studio) | (existing message) | `is_deleted = true` on the color row | color removed from list | ✓ |
| 11 | Add a feature | "Đã thêm điểm nổi bật." | new row in `motorcycle_features` | feature list updates | ✓ |
| 12 | Edit a feature | "Đã cập nhật tính năng." | feature row updated | feature list updates | ✓ |
| 13 | Delete a feature | "Đã xóa điểm nổi bật." | `is_deleted = true` | feature removed from list | ✓ |
| 14 | Delete the motorcycle (Xoa page) | "Đã xóa xe" | `is_deleted = true` | row gone from the list | ✓ |

### Blog (admin)

| # | Action | Expected success message | DB persistence check | Re-render check | Toast check |
| - | --- | --- | --- | --- | --- |
| 1 | Add a new post | "Đã thêm bài viết" | new row in `blog_posts` | row appears in the list | ✓ |
| 2 | Change the title | "Đã cập nhật bài viết" | `title` updated | title echo in input | ✓ |
| 3 | Change the content | "Đã cập nhật bài viết" | `content` updated | textarea echoes new text | ✓ |
| 4 | Confirm line breaks preserved in the content | n/a | `content` stored with `\n` | public detail page renders as separate paragraphs (existing public-site behaviour) | n/a |
| 5 | Upload a new thumbnail | "Đã cập nhật bài viết" | `thumbnail_url` updated | preview reloads | ✓ |
| 6 | Change the category | "Đã cập nhật bài viết" | `category_id` updated | dropdown shows the new option | ✓ |
| 7 | Toggle publish (IsPublished checkbox) | "Đã cập nhật bài viết" | `is_published` flips | badge flips | ✓ |
| 8 | Delete a post (list page Xóa button) | "Đã xóa bài viết" | `is_deleted = true` | row removed from the list | ✓ |

### Toast / modal

| Check | Expected |
| --- | --- |
| Toast appears bottom-right on desktop, top on mobile | ✓ |
| Toast does not block important action buttons | ✓ (floats, pointer-events: none on the stack) |
| Toast auto-dismisses after ~4.5 s for success | ✓ |
| Error toasts are sticky until the user closes them | ✓ |
| Manual close (`&times;`) works | ✓ |
| `data-confirm` modal replaces native `window.confirm()` | ✓ |
| `_AdminFlash` fallback banner is still rendered for users with JS disabled | ✓ |
| Success message is NOT shown when `SaveChangesAsync` throws | ✓ (verified by source path; `SetSuccess` only runs after `await uow.SaveChangesAsync(ct)`) |

---

## 11. Remaining risks

1. **No live PostgreSQL integration test.** The InMemory provider does not
   catch Npgsql-specific edge cases. Run the full suite against PostgreSQL
   before cutting the release.
2. **`admin.js` and `admin-toast.js` are loaded synchronously** in the
   admin layout. If the browser has JS disabled, the inline banner is shown
   and the modal/toast are skipped. The fallback is fine; just note the
   assumption.
3. **The SEO tab's `OnPostSaveSeoAsync` handler is still active** for any
   tool that POSTs to `/admin/xe/editor/{id}?handler=SaveSeo`. The handler
   itself is untouched because the public site still reads those columns.
   If a future refactor removes the SEO tab, also remove the handler.
4. **The motorcycle listing still shows a `Xóa` link to the Xoa page.**
   The Xoa page is the soft-delete confirmation. The body now matches the
   spec. If the staff prefers a modal, swap the link for a `[data-confirm]`
   form button that POSTs to the same Xoa handler — this would require an
   `OnPostDeleteAsync` in `XoaModel` accepting a redirect, which is
   already implicit in the current `OnPostAsync` POST handler.
5. **Toast duration is hard-coded.** If the staff feedback suggests a
   longer or shorter window, edit the `TOAST_DURATION_BY_TYPE` table in
   `wwwroot/js/admin-toast.js`.
6. **No browser-level manual test was performed in this phase.** The
   matrix in §10 must be ticked off by hand on the staging environment
   before handover.

---

## 12. Production sign-off

- Build: PASS (0 errors, 8 pre-existing warnings).
- Test suite: PASS (132/132).
- Manual test matrix: NOT YET RUN (see §10, requires staging browser).
- Migration applied: NO (none required).
- Public site: UNCHANGED.

**Do not declare production PASS until the §10 matrix is ticked off in a
real browser session.**
