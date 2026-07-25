# 02 — Solution Structure

## Solution file

`HieuNga.sln` contains:

| Project | Path | Role |
|---------|------|------|
| HieuNga.Domain | `src/HieuNga.Domain` | Core domain |
| HieuNga.Application | `src/HieuNga.Application` | Application services |
| HieuNga.Infrastructure | `src/HieuNga.Infrastructure` | Persistence & integrations |
| HieuNga.Web | `src/HieuNga.Web` | Host / UI |
| HieuNga.Tests | `tests/HieuNga.Tests` | Unit tests |

## Dependency graph

```
HieuNga.Web
  ├── HieuNga.Application
  │     └── HieuNga.Domain
  └── HieuNga.Infrastructure
        ├── HieuNga.Application
        └── HieuNga.Domain

HieuNga.Tests
  └── HieuNga.Application
```

**Rules observed:**
- Domain has zero project references.
- Application depends only on Domain.
- Infrastructure depends on Application + Domain (implements interfaces).
- Web depends on Application + Infrastructure (composition root).
- Tests currently only reference Application (very thin coverage).

## Startup order

1. Process starts `HieuNga.Web` → `Program.cs`.
2. Optional `PORT` → rewrite `ASPNETCORE_URLS`.
3. `AddApplication()` then `AddInfrastructure(configuration)`.
4. Web-specific DI (cookies, Razor Pages, filters, compression).
5. Build `WebApplication`.
6. Middleware pipeline.
7. Map `/health` + Razor Pages.
8. **Blocking:** `DbInitializer.InitializeAsync` (migrate + seed/sync). Failure aborts start.
9. `app.Run()`.

There is **no** multi-host orchestrator; Docker Compose may run `postgres` + `web` (+ optional `nginx`) for local stacks.

## Per-project detail

### HieuNga.Domain

**Purpose:** Pure business model and contracts.

**Contains:**
- `Entities/` — 19 domain entities
- `Enums/` — 6 enums
- `Common/` — `BaseEntity`, `ISeoEntity`
- `Interfaces/` — `IRepository<>`, `IUnitOfWork`, specialized repos

**Dependencies:** none  
**Packages:** none

### HieuNga.Application

**Purpose:** Use cases for public site (and shared contracts used by Infrastructure).

**Contains:**
- `Services/` — Homepage, Motorcycle, Booking, Installment, Promotion, Blog, Branch
- `Interfaces/` — service contracts + `IImageStorageService`, finance/site catalog interfaces
- `DTOs/` — request/response shapes for UI
- `Validators/` — FluentValidation (booking only)
- `Mappings/` — entity→DTO + motorcycle image catalog
- `Options/` — `SiteOptions`, `ImageStorageOptions`

**Dependencies:** Domain  
**Packages:** FluentValidation 11.11

### HieuNga.Infrastructure

**Purpose:** Technical implementations.

**Contains:**
- `Persistence/` — DbContext, configurations, migrations, DbInitializer, seeds, enrichers
- `Repositories/` — generic + specialized
- `Identity/` — `ApplicationUser`
- `Services/` — ServiceCatalog, FinanceConfig, SiteSettings, Image storage
- `DependencyInjection.cs`

**Dependencies:** Application, Domain  
**Packages:** EF Core Identity, Npgsql, CloudinaryDotNet, JwtBearer *(referenced, unused)*

### HieuNga.Web

**Purpose:** HTTP host and UI.

**Contains:**
- `Program.cs` — composition root
- `Pages/` — public + Admin Razor Pages
- `Middleware/`, `Filters/`, `Services/`, `Extensions/`
- `wwwroot/` — CSS/JS/images
- `appsettings*.json`

**Dependencies:** Application, Infrastructure  
**Packages:** EF Design (migrations tooling)

### HieuNga.Tests

**Purpose:** Automated tests.

**Current state:** Placeholder xUnit project with essentially empty tests. Not a meaningful safety net yet.

## Supporting folders (outside projects)

| Path | Purpose |
|------|---------|
| `docker/` | Local Compose (postgres, web, nginx) |
| `Dockerfile` | Multi-stage production image |
| `render.yaml` | Optional Render Blueprint |
| `docs/` | Deploy docs + this architecture set |
| `.env.example` | Env var template |

## Naming note

Solution/project/namespace names use **HieuNga**. Customer-facing brand is **Xe Máy Hiếu Nga**. Do not rename projects for branding.
