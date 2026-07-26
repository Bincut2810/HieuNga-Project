# Phase 4 — Admin UX Audit (pre-implementation)

**Scope:** UI/UX only. No business logic, routing, API, schema, or repository changes.

## Dual system found

| Island | Style | Pages |
|--------|-------|-------|
| Modern | `admin.css` tokens | Dashboard, Xe list, Editor, Media Studio, some lists |
| Legacy | Tailwind class names **without Tailwind loaded** | CaiDat, TraGop, KhachHang, most content forms, Xoa, DanhMuc |

## Layouts

- Single shell: `Pages/Admin/Shared/_AdminLayout.cshtml`
- Login island: `DangNhap.cshtml` (Tailwind CDN — keep)

## Navigation

Hardcoded in `_AdminLayout` — groups: Tổng quan, Inventory, Service, Finance, Content, Site.

## Assets

- `wwwroot/css/admin.css` (~1600 lines) — tokens + shell + `mm-*` legacy + `ms-*` Media Studio
- `wwwroot/js/admin.js` — sidebar drawer
- Editor-only: `motorcycle-editor.js`, `media-studio.js`, `motorcycle-content.js`

## Unused / dead UI (candidates)

- Unused partials: `_AdminFormCard*`, `_AdminTableWrap*`, `_AdminUploadField` (wire or keep as system)
- Orphan CSS: `.admin-toast`, large unused `.mm-toolbar` / `.mm-root` suites
- ImportDemo inline badge CSS redefinitions

## Must preserve

- Media Studio (`ms-*`, `media-studio.js`, `/admin/api/xe/{id}/media`)
- Editor routes/handlers/`?tab=`
- Legacy redirects: them/sua/gia/noi-dung
- All `@page` routes and form handlers

## Target hierarchy

```
Admin Shell (sidebar icons + collapse)
├── Design system (space/type/card/form/btn/badge/upload/empty/table/modal/sticky)
├── Dashboard control center
├── Inventory list (media completeness cards)
├── Motorcycle editor workspace + checklist + sticky bar
└── Unified Content / Service / Finance / Settings forms
```
