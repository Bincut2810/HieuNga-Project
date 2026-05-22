# Honda Hiếu Nga — Architecture

## Solution Structure

```
HieuNga/
├── src/
│   ├── HieuNga.Domain/          # Entities, enums, repository contracts
│   ├── HieuNga.Application/     # DTOs, services, validators, mappings
│   ├── HieuNga.Infrastructure/ # EF Core, repositories, Identity, seed
│   └── HieuNga.Web/             # Razor Pages, UI, HTMX partials
├── tests/HieuNga.Tests/
├── docker/                      # Compose, Nginx, Dockerfile
└── docs/
```

## Layer Responsibilities

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Business entities, enums, `IRepository` contracts. No framework dependencies. |
| **Application** | Use cases via services, DTOs for UI/API, FluentValidation, entity→DTO mapping. |
| **Infrastructure** | PostgreSQL via EF Core, repository implementations, Identity admin auth, migrations, seed. |
| **Web** | Razor Pages, HTMX partial navigation, Alpine.js micro-interactions, SEO ViewData, static assets. |

## Key UX Decisions

1. **HTMX `hx-boost`** on `<body>` — full-page feel without full reload; swaps `#main-content` only.
2. **Sticky mobile CTA** — Call / Installment / Test ride always visible (conversion-first).
3. **Skeleton & lazy images** — catalog ready for loading states; `loading="lazy"` on cards.
4. **Structured data** — JSON-LD for dealer (layout) and product (detail page).

## Database Tables (PostgreSQL)

- `motorcycles`, `motorcycle_variants`, `motorcycle_colors`
- `promotions`, `branches`, `bookings`, `maintenance_bookings`, `installment_requests`
- `blog_categories`, `blog_posts`, `reviews`, `media_assets`, `banners`, `site_settings`
- `admins` (+ Identity role tables)

Soft delete via `IsDeleted` + global query filters on core entities.

## Expansion Path

- **Admin area** (`/admin`) — Razor Pages or minimal API behind Identity cookie auth.
- **Compare** — session/cookie store motorcycle IDs; partial HTMX panel.
- **CMS** — CRUD services mirroring public repositories.
- **CDN** — move `media_assets` to S3-compatible storage with signed URLs.
