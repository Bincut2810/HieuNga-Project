# 01 — System Overview

> Phase 1 Architecture Audit (read-only). Brand (customer-facing): **Xe Máy Hiếu Nga**. Solution/namespaces remain `HieuNga.*`.

## Project purpose

Digital showroom and CMS for a motorcycle dealership in Đà Nẵng. The system serves:

1. **Public website** — catalog, motorcycle detail, installment calculator, maintenance booking, contact, promotions, news, compare.
2. **Admin CMS** — manage motorcycles, content, service catalog, finance partners, customer leads, site settings.

It is **not** a full ERP (no inventory transactions, no invoicing, no payment gateway, no warranty workflow, no workshop work-order engine).

## Overall architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Browser (Public / Admin)                │
│         Tailwind CDN · Alpine.js · HTMX · Inter font        │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTPS
┌───────────────────────────▼─────────────────────────────────┐
│                    HieuNga.Web (host)                       │
│         ASP.NET Core 8 Razor Pages monolith                 │
│  Middleware → Auth → Razor Pages / Minimal /health          │
└───────┬─────────────────────────────┬───────────────────────┘
        │                             │
        ▼                             ▼
┌───────────────────┐       ┌───────────────────────────────┐
│ HieuNga.Application│       │ Direct DbContext / IRepository│
│ Services + DTOs    │       │ (Admin CMS PageModels)        │
└─────────┬─────────┘       └───────────────┬───────────────┘
          │                                 │
          └────────────┬────────────────────┘
                       ▼
┌──────────────────────────────────────────────────────────────┐
│                 HieuNga.Infrastructure                         │
│  EF Core · Repositories · Identity · Seed · Image storage    │
└───────────────────────────┬──────────────────────────────────┘
                            ▼
┌──────────────────────────────────────────────────────────────┐
│              PostgreSQL 16  (+ optional Cloudinary)          │
└──────────────────────────────────────────────────────────────┘
```

**Style:** Clean Architecture–inspired **4-layer** solution, hosted as a **single deployable** (`HieuNga.Web`).

**Important deviation:** Public read/lead flows mostly go through Application services; **Admin CMS writes often use `HieuNgaDbContext` / `IRepository<>` directly from PageModels**, bypassing Application services.

## Technology stack

| Area | Choice |
|------|--------|
| Runtime | .NET 8 (`net8.0`) |
| UI | ASP.NET Core Razor Pages |
| ORM | EF Core 8 + Npgsql |
| Database | PostgreSQL 16 |
| Auth | ASP.NET Core Identity (cookie) |
| Validation | FluentValidation (partial) + DataAnnotations |
| Frontend | Tailwind CDN, Alpine.js 3, HTMX 2 |
| Images | Local disk and/or Cloudinary |
| Hosting | Docker → Render (or similar) |
| Tests | xUnit (minimal) |

## Runtime model

- One Kestrel process (`HieuNga.Web.dll`).
- On startup: apply EF migrations + seed/sync (`DbInitializer`).
- Bind address: `0.0.0.0` + `PORT` (Render) or `ASPNETCORE_URLS`.
- No separate API host, worker process, or message bus.

## Database

- Single PostgreSQL database.
- Identity tables renamed to `admins*` prefix.
- Domain tables use snake_case plural names (e.g. `motorcycles`, `finance_rates`).
- Soft delete (`IsDeleted`) on `BaseEntity`; global query filters on most catalog entities (not on all lead/settings tables).

## Main modules

| Module | Public | Admin |
|--------|--------|-------|
| Motorcycles / catalog | `/xe`, `/xe/{slug}`, `/so-sanh` | `/admin/xe/*` |
| Installment | `/tra-gop`, detail calculator | `/admin/tra-gop/*`, lead inbox |
| Maintenance / services | `/bao-duong*` | `/admin/dich-vu/*`, lead inbox |
| Leads (bookings) | forms on public pages | `/admin/khach-hang/*` |
| Content (promo, news, banner, branch) | `/khuyen-mai`, `/tin-tuc` | matching Admin CRUD |
| Site settings / SEO | layout meta | `/admin/cai-dat` |
| Auth | — | `/admin/dang-nhap` |

## Layer responsibilities

| Layer | Project | Responsibility |
|-------|---------|----------------|
| Domain | `HieuNga.Domain` | Entities, enums, repository contracts. No NuGet, no framework. |
| Application | `HieuNga.Application` | Use-case services, DTOs, validators, mappings. |
| Infrastructure | `HieuNga.Infrastructure` | EF, Identity, repositories, seed, Cloudinary/local storage. |
| Presentation | `HieuNga.Web` | Razor Pages, middleware, filters, static assets, DI composition. |
| Tests | `HieuNga.Tests` | Minimal xUnit; references Application only. |

## What this system is / is not

| Is | Is not |
|----|--------|
| Marketing + lead-capture showroom | ERP / DMS |
| CMS for bikes & content | Inventory stock ledger |
| Installment **estimator** | Loan origination / bank API |
| Service catalog + booking form | Workshop WO / parts billing |
| Cookie Identity for Admin | Multi-tenant SaaS / JWT API |

## Related docs

- [02_SOLUTION_STRUCTURE.md](02_SOLUTION_STRUCTURE.md)
- [03_DATABASE.md](03_DATABASE.md)
- [12_AI_CONTEXT.md](12_AI_CONTEXT.md)
