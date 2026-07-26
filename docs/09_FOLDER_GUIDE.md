# 09 — Folder Guide

## Repository root

| Path | Purpose |
|------|---------|
| `HieuNga.sln` | Solution |
| `Dockerfile` | Production multi-stage build |
| `render.yaml` | Optional Render Blueprint |
| `.dockerignore` / `.gitignore` | Build/VCS hygiene |
| `.env.example` | Env template (no secrets) |
| `README.md` | Quick start |
| `package.json` | Optional front-end tooling remnant (Tailwind primarily via CDN) |
| `docker/` | Local compose stack |
| `docs/` | Operations + architecture docs |
| `src/` | Product code |
| `tests/` | Test projects |

## `src/HieuNga.Domain`

| Folder | Contents |
|--------|----------|
| `Entities/` | Domain models |
| `Enums/` | Domain enums |
| `Common/` | `BaseEntity`, `ISeoEntity` |
| `Interfaces/` | Repository contracts |

## `src/HieuNga.Application`

| Folder | Contents |
|--------|----------|
| `Services/` | Use-case implementations |
| `Interfaces/` | Application + shared infra contracts |
| `DTOs/` | UI/service contracts |
| `Validators/` | FluentValidation |
| `Mappings/` | Entity mappers, image catalog |
| `Options/` | Strongly typed options |

## `src/HieuNga.Infrastructure`

| Folder | Contents |
|--------|----------|
| `Persistence/` | DbContext, configs, migrations, seed |
| `Persistence/Configurations/` | Fluent API entity configs |
| `Persistence/Migrations/` | EF migrations + snapshot |
| `Repositories/` | EF repository implementations |
| `Identity/` | `ApplicationUser` |
| `Services/` | Catalog, finance, settings, image storage |
| `DependencyInjection.cs` | Infra DI entry |

## `src/HieuNga.Web`

| Folder | Contents |
|--------|----------|
| `Pages/` | Public Razor Pages by feature |
| `Pages/Admin/` | CMS (Vietnamese route segments) |
| `Pages/Shared/` | Layouts/partials |
| `Middleware/` | SEO robots middleware |
| `Filters/` | Site settings page filter |
| `Services/` | Web-only helpers (compare cookie, upload helper) |
| `Extensions/` | SEO/price helpers |
| `wwwroot/css` | site.css, admin.css |
| `wwwroot/js` | polish.js, finance-calculator.js, detail-viewer.js, homepage.js |
| `wwwroot/images` | Static demo assets |
| `wwwroot/uploads` | Local uploads (gitignored) |
| `Views/Shared/Components/` | Optional ViewComponents |
| `Properties/` | launchSettings |

### Public page folders (by route family)

| Folder | Routes |
|--------|--------|
| `Pages/Xe` | Catalog/detail |
| `Pages/BaoDuong` | Services |
| `Pages/TraGop` | Installment calculator |
| `Pages/KhuyenMai` | Promotions |
| `Pages/TinTuc` | News |
| `Pages/LienHe` | Contact |
| `Pages/SoSanh` | Compare |
| `Pages/DatLichLaiThu` | Test ride |

### Admin page folders

| Folder | Concern |
|--------|---------|
| `Admin/Xe` | Motorcycles |
| `Admin/Banner`, `ChiNhanh`, `KhuyenMai`, `TinTuc` | Content |
| `Admin/DichVu` | Service catalog |
| `Admin/TraGop` | Banks/rates |
| `Admin/KhachHang` | Lead inboxes |
| `Admin/CaiDat` | Settings |
| `Admin/Shared` | Admin layout/forms |

## `docker/`

| Item | Purpose |
|------|---------|
| `docker-compose.yml` | postgres + web + nginx |
| `nginx/` | Local reverse proxy config |

## `docs/`

| Doc set | Role |
|---------|------|
| `01`–`12` architecture docs | This audit |
| `ENVIRONMENT.md`, `DEPLOY-RENDER.md`, `STAGING-CHECKLIST.md` | Ops |
| `ARCHITECTURE.md` | Older short architecture note (superseded in depth by `01`+) |

## `tests/HieuNga.Tests`

Placeholder unit test project — expand here for Application service tests first.
