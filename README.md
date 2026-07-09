# Honda Hiếu Nga Đà Nẵng — Digital Showroom Platform

Production-grade motorcycle dealership platform for **Honda Hiếu Nga HEAD Đà Nẵng**.

## Tech Stack

- ASP.NET Core 8 + Razor Pages
- PostgreSQL + Entity Framework Core
- Clean Architecture + Repository + Service Layer
- TailwindCSS + Alpine.js + HTMX
- Docker + Nginx

## Quick Start

### Prerequisites

- .NET 8 SDK
- Docker (optional, for PostgreSQL)

### 1. Database

```powershell
cd docker
docker compose up postgres -d
```

### 2. Run application

```powershell
cd src/HieuNga.Web
dotnet ef database update --project ../HieuNga.Infrastructure
dotnet run
```

Open http://localhost:5000 (or the URL shown in console).

### 3. Full stack (Docker)

```powershell
cd docker
docker compose up --build
```

- Site: http://localhost
- API/Web container: http://localhost:8080

### Default admin (development seed only)

Local Development uses credentials from `appsettings.Development.json` or [docs/ENVIRONMENT.md](docs/ENVIRONMENT.md).

**Production:** set `SeedOptions__AdminSeedEnabled`, `SeedOptions__AdminEmail`, and `SeedOptions__AdminPassword` (12+ characters) on first deploy. Set `SeedOptions__AdminSeedEnabled=false` after login works. No default admin is created without them.

**Staging checklist:** [docs/STAGING-CHECKLIST.md](docs/STAGING-CHECKLIST.md)

## Project Structure

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Features

### Public site
- [x] Homepage, catalog, detail, installment, test ride
- [x] Promotions (`/khuyen-mai`, `/khuyen-mai/{slug}`)
- [x] News (`/tin-tuc`, `/tin-tuc/{slug}`)
- [x] Contact (`/lien-he`) — hotline, Zalo, map, consultation form
- [x] Compare (`/so-sanh`) — cookie-based, up to 3 bikes
- [x] Maintenance booking (`/bao-duong`)

### Admin CMS (`/admin`)
- [x] Login (`/admin/dang-nhap`)
- [x] Dashboard + list views: xe, khuyến mãi, tin tức, banner, chi nhánh
- [ ] Full CRUD forms (Phase 2)

## Configuration

See [docs/ENVIRONMENT.md](docs/ENVIRONMENT.md) for all environment variables.

- Local: `src/HieuNga.Web/appsettings.Development.json` — local Docker postgres (port 5433).
- Production: set `ConnectionStrings__DefaultConnection` on Render (see [docs/DEPLOY-RENDER.md](docs/DEPLOY-RENDER.md)).

## Deploy to Render.com

**Full step-by-step guide (Vietnamese):** [docs/DEPLOY-RENDER.md](docs/DEPLOY-RENDER.md)

Quick: push to GitHub → Render PostgreSQL → Render Web Service (Docker) → set env vars (see [ENVIRONMENT.md](docs/ENVIRONMENT.md)) → verify with [STAGING-CHECKLIST.md](docs/STAGING-CHECKLIST.md).
