# Phase 6.0 — Startup simplification (demo seed removal)

Architecture cleanup: production startup no longer creates motorcycles, demo content, or inventory fillers.

## Old startup

```
Program.cs
  └─ DbInitializer.InitializeAsync
       ├─ Database.MigrateAsync()
       ├─ SeedInitialAsync (motorcycles + banners + reviews + site settings)  [demo]
       ├─ SeedAdminUserAsync
       ├─ HieuNgaBranchSeed.EnsureAsync
       ├─ SeedDemoContentAsync (promotions + blogs)                            [demo]
       ├─ ServiceFinanceSeed.SeedAsync (services + banks + branding repair)
       ├─ HieuNgaServiceExperienceSeed.EnsureAsync (+ legacy demo deactivate)
       ├─ HieuNgaInventorySeed.EnsureAsync  ← inserts DemoCatalogDefinitions  [demo]
       ├─ HieuNgaHeroBannerSeed.EnsureAsync                                   [demo]
       └─ MotorcycleContentEnricher.EnrichAsync                               [demo]
```

Admin path (separate): `/admin/xe/import-demo` → `IDemoMotorcycleImporter` → `DemoAssets/`.

### Old dependency graph (demo motorcycle path)

```
DbInitializer
  ├─ HieuNgaInventorySeed
  │    ├─ HieuNgaInventoryTargets
  │    ├─ DemoCatalogDefinitions
  │    ├─ DemoPackageCatalog / DemoMotorcycleMetadata
  │    └─ MotorcycleImageCatalog.GetThumbnail (demo SVG map)
  ├─ MotorcycleContentEnricher → MotorcycleContentCatalog
  ├─ SeedInitialAsync → CreateMotorcycle (fixed slugs)
  └─ (Admin) DemoMotorcycleImporter → DemoAssets + /demo-assets static files
```

## New startup

```
Program.cs
  └─ DbInitializer.InitializeAsync
       ├─ Database.MigrateAsync()   (retry on transient DB errors)
       └─ SeedAdminUserAsync        (ops bootstrap only; never motorcycles)
Application starts
```

Motorcycle lifecycle (CMS is SSoT):

```
Admin → Create Motorcycle → Upload Images → Publish → Public Site
```

Zero automatic motorcycle creation. Zero startup inserts into `motorcycles`.

## Classification

| Component | Decision | Justification |
|-----------|----------|---------------|
| `Database.MigrateAsync` | **KEEP** | Schema evolution |
| `SeedAdminUserAsync` | **KEEP** | First-deploy login only; gated by env; not CMS content |
| `SeedOptions` (Admin*) | **KEEP** | Supports admin bootstrap |
| `SeedOptions.EnableDemoSeed` / `RunContentEnricher` | **REMOVE** | Demo flags |
| `SeedInitialAsync` / `CreateMotorcycle` | **REMOVE** | Demo bikes + Unsplash banners |
| `SeedDemoContentAsync` | **REMOVE** | Demo promotions/blogs |
| `HieuNgaInventorySeed` | **REMOVE** | Startup motorcycle inserts from demo catalog |
| `HieuNgaInventoryTargets` | **REMOVE** | Demo inventory quotas |
| `MotorcycleContentEnricher` + `MotorcycleContentCatalog` | **REMOVE** | Overwrote CMS fields |
| `HieuNgaHeroBannerSeed` | **REMOVE** | Default/demo banners |
| `HieuNgaBranchSeed` | **REMOVE** | CMS manages branches; showroom catalog kept for display fallbacks |
| `HieuNgaServiceExperienceSeed` + `HieuNgaServiceExperience` | **REMOVE** | Startup service inserts + legacy demo repair |
| `ServiceFinanceSeed` | **REMOVE** | Startup inserts + branding repair mutating motorcycles; banks/services via Admin |
| `IDemoMotorcycleImporter` / `DemoMotorcycleImporter` | **REMOVE** | Demo import pipeline |
| `DemoCatalogDefinitions` / `DemoPackageCatalog` / metadata | **REMOVE** | Demo-only |
| Admin `ImportDemo` page + nav link | **REMOVE** | Demo UI |
| `DemoAssets/` + `/demo-assets` static map | **REMOVE** | Demo media |
| Demo motorcycle SVG/JPG placeholders | **REMOVE** | Keep `default.svg` / `default.jpg` only |
| `MotorcycleImageCatalog` demo slug map | **REMOVE** | Keep `Default` + CMS URL validation only |
| `HieuNgaShowrooms` | **KEEP** | Display/contact fallbacks (not a seed) |
| DI `IDemoMotorcycleImporter` registration | **REMOVE** | Dead |

## Duplicate slug (`IX_motorcycles_Slug`) — root cause

**Insert path:** `HieuNgaInventorySeed.EnsureAsync` (always called from `DbInitializer`, every startup).

1. Seed walked `DemoCatalogDefinitions.All` and `Add`ed motorcycles with fixed slugs (e.g. `demo-scooter-01`, Vision/SH package slugs, fillers `demo-{category}-fill-N`).
2. Existence check used `Slug == meta.Slug && !m.IsDeleted`. Soft-deleted rows with the same slug were treated as “missing”, so a **second row with the same slug** was inserted → unique index `IX_motorcycles_Slug` violated.
3. Parallel path: `SeedInitialAsync` could also insert `honda-vision-2025` / `honda-sh-160i` / etc. when the motorcycle table was empty and demo seed was enabled — overlapping slug space with catalog/enricher repair paths increased risk on redeploys.

**After Phase 6:** no startup code inserts into `motorcycles`. Slug uniqueness is enforced only by Admin create/update flows + the DB unique index.

## Deleted artifacts

### Classes / services

- `HieuNgaInventorySeed`, `MotorcycleContentEnricher`, `MotorcycleContentCatalog`
- `HieuNgaHeroBannerSeed`, `HieuNgaBranchSeed`, `HieuNgaServiceExperienceSeed`, `ServiceFinanceSeed`
- `DemoMotorcycleImporter`, `IDemoMotorcycleImporter`, `DemoCatalogDefinitions`, `DemoPackageCatalog`, `DemoMotorcycleMetadata`, `DemoCatalogSeedResult`
- `HieuNgaInventoryTargets`, `HieuNgaServiceExperience`
- Admin `ImportDemoModel` + page

### Registrations / config

- `services.AddScoped<IDemoMotorcycleImporter, DemoMotorcycleImporter>()`
- `Program.cs` `/demo-assets` static files
- `SeedOptions__EnableDemoSeed`, `SeedOptions__RunContentEnricher` (appsettings, `.env.example`, `render.yaml`, docker-compose)
- `HieuNga.Web.csproj` `DemoAssets` copy-to-output

### Docs / tests / assets

- `docs/DEMO_IMPORT_SYSTEM.md`
- `docs/DemoAssets/**`, `src/HieuNga.Web/DemoAssets/**`
- `tests/.../DemoCatalogDefinitionsTests.cs`, `DemoPackageCatalogTests.cs`
- Demo bike images under `wwwroot/images/motorcycles/honda-*`

### Repair logic removed

- Inventory ensure / republish / synthetic fillers
- Content enricher overwrite
- Legacy demo service deactivation on startup
- `ServiceFinanceSeed.MigrateLegacyBrandingAsync` (including motorcycle meta rewrite)
- Branch placeholder auto-replace on startup

## New architecture (dependency graph)

```
Program.cs
  └─ DbInitializer
       ├─ EF Core MigrateAsync
       └─ Identity Admin seed (optional)
            └─ SeedOptions / AdminSeed__* env aliases

CMS (Admin Razor Pages)
  └─ Motorcycle create / media studio / publish
       └─ Public pages read DB only
```

## Ops notes

- **Fresh empty DB:** migrations apply; site starts empty. Create admin (dev defaults or `AdminSeed*`), then create motorcycles, banks, services, banners, branches in CMS.
- **Existing production DB:** starts normally; existing CMS data unchanged; no startup motorcycle inserts.
- After first successful admin login in production, set `SeedOptions__AdminSeedEnabled=false`.
