# 03 — Database

## Technology

- **RDBMS:** PostgreSQL 16
- **ORM:** Entity Framework Core 8 + Npgsql
- **DbContext:** `HieuNgaDbContext` (`IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`)
- **Location:** `src/HieuNga.Infrastructure/Persistence/`

## DbContext overview

**19 domain DbSets** + ASP.NET Identity tables.

Identity table renames:

| Identity concept | Table |
|------------------|-------|
| Users | `admins` |
| Roles | `admin_roles` |
| UserRoles | `admin_user_roles` |
| UserClaims | `admin_user_claims` |
| UserLogins | `admin_user_logins` |
| RoleClaims | `admin_role_claims` |
| UserTokens | `admin_user_tokens` |

Configurations applied via `ApplyConfigurationsFromAssembly`.

**No** `SaveChanges` interceptor for automatic auditing — `UpdatedAt` is set in `Repository.UpdateAsync` / `SoftDeleteAsync`.

## Base entity & audit

Every domain entity inherits `BaseEntity`:

| Field | Type | Notes |
|-------|------|-------|
| `Id` | Guid | Default `Guid.NewGuid()` |
| `CreatedAt` | DateTime | Default UtcNow on construction |
| `UpdatedAt` | DateTime? | Set on update/soft-delete via repository |
| `IsDeleted` | bool | Soft delete flag |

**No** `CreatedBy` / `UpdatedBy`.

## Soft delete

| Pattern | Detail |
|---------|--------|
| Column | Present on all domain tables |
| API | `IRepository.SoftDeleteAsync` |
| Query filters | `HasQueryFilter(x => !x.IsDeleted)` on most catalog entities |

**Entities WITHOUT global query filter** (still have column):

- `Booking`
- `MaintenanceBooking`
- `InstallmentRequest`
- `Banner`
- `SiteSetting`

## SEO interface

`ISeoEntity`: MetaTitle, MetaDescription, MetaKeywords, OgImageUrl, CanonicalUrl.

**Implementers:** Motorcycle, BlogPost, BlogCategory, Promotion, ServiceItem.

## Enums (stored as int)

| Enum | Values |
|------|--------|
| `MotorcycleCategory` | Scooter, Sport, Naked, Adventure, Cub, Electric, Other=99 |
| `BannerPosition` | Hero, HomepageMid, Catalog, Promotion |
| `BookingType` | TestRide, Maintenance, Consultation |
| `BookingStatus` | Pending, Confirmed, Completed, Cancelled |
| `PromotionType` | Discount, Gift, Financing, TradeIn, Event |
| `MediaType` | Image, Video, Document |

## Entity catalog (19)

### Catalog / showroom

| Entity | Table | Key relations |
|--------|-------|---------------|
| Motorcycle | `motorcycles` | 1→N Variants, Colors, MediaAssets, Reviews |
| MotorcycleVariant | `motorcycle_variants` | FK Motorcycle (Cascade) |
| MotorcycleColor | `motorcycle_colors` | FK Motorcycle (Cascade) |
| MediaAsset | `media_assets` | optional FK Motorcycle |
| Review | `reviews` | FK Motorcycle (Cascade) |

Motorcycle extras: `HighlightsJson`, `TechnicalSpecsJson` (text JSON), unique `Slug`, `BasePrice` precision (18,0).

### Content / marketing

| Entity | Table | Notes |
|--------|-------|-------|
| Banner | `banners` | No soft-delete filter |
| Promotion | `promotions` | optional Motorcycle FK; unique Slug |
| BlogCategory | `blog_categories` | 1→N Posts |
| BlogPost | `blog_posts` | optional Category FK; unique Slug |
| Branch | `branches` | unique Slug |
| SiteSetting | `site_settings` | unique `Key`; KV store |

### Leads / CRM-lite

| Entity | Table | Notes |
|--------|-------|-------|
| Booking | `bookings` | TestRide / Consultation (+ Type enum); optional Motorcycle/Branch |
| MaintenanceBooking | `maintenance_bookings` | Dedicated maintenance form; `MotorcycleModel` is **string**, not FK |
| InstallmentRequest | `installment_requests` | Lead capture for finance |

All three have `AdminNotes` (added in AdminCmsServiceFinance migration).

### Service catalog

| Entity | Table | Notes |
|--------|-------|-------|
| ServiceCategory | `service_categories` | Restrict delete to items |
| ServiceItem | `service_items` | Price as **text** (`EstimatedPriceText`); IncludesJson |

### Finance partners

| Entity | Table | Notes |
|--------|-------|-------|
| BankType | `bank_types` | |
| Bank | `banks` | Restrict to BankType |
| FinanceRate | `finance_rates` | Cascade from Bank; `MonthlyInterestRatePercent` (8,4) |

### Identity (Infrastructure)

`ApplicationUser`: FullName, IsActive, CreatedAt, LastLoginAt + IdentityUser fields.

## Indexes / constraints (notable)

**Unique:** motorcycle/promotion/blog/branch/service/bank-type slugs; `site_settings.Key`; Identity normalized usernames/roles.

**FK delete behaviors:**
- Motorcycle → variants/colors/reviews: Cascade
- Bank → rates: Cascade
- BankType → Bank / ServiceCategory → ServiceItem: Restrict

## Migration history

| Migration | Purpose |
|-----------|---------|
| `20260522043349_InitialCreate` | Core schema + Identity |
| `20260522165022_MotorcycleContentFields` | Motorcycle Highlights/Specs JSON |
| `20260623043122_AdminCmsServiceFinance` | Services, banks/rates, AdminNotes |

Snapshot: `HieuNgaDbContextModelSnapshot.cs`.

## ER explanation (textual)

```
Motorcycle ──┬── MotorcycleVariant
             ├── MotorcycleColor
             ├── MediaAsset (optional)
             ├── Review
             ├── Promotion (optional)
             ├── Booking (optional)
             └── InstallmentRequest (optional)

Branch ──┬── Booking (optional)
         └── MaintenanceBooking (optional)   [no Branch.MaintenanceBookings nav]

BlogCategory ── BlogPost (optional category)

ServiceCategory ── ServiceItem

BankType ── Bank ── FinanceRate

Standalone: Banner, SiteSetting

admins (ApplicationUser) — no FK into domain leads
```

## Seed / sync behavior (startup)

`DbInitializer` + `ServiceFinanceSeed`:

1. `MigrateAsync` with retry.
2. Optional demo seed (`EnableDemoSeed` / Development).
3. Admin user seed (gated in Production).
4. Service catalog / banks if empty; **idempotent bank sync** for HD Bank / MB Bank / JACCS.
5. Legacy branding migration for default site settings.
6. Optional motorcycle content enricher (Development / flag).

## Gaps / domain notes

- Dual maintenance concepts: `BookingType.Maintenance` vs `MaintenanceBooking` entity (public form uses the latter).
- Prices for services are display strings, not numeric money types.
- No inventory movements, invoices, payments, or warranty tables exist.
