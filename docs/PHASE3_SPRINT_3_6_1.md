# Sprint 3.6.1 — Complete Inventory & Category Experience

**Date:** 2026-07-25

## Audit — MotorcycleCategory enum

| Enum | Public label |
|------|----------------|
| `Scooter` | Xe tay ga |
| `XeSo` | Xe số |
| `ConTay` | Xe côn tay |
| `PhanKhoiLon` | Xe phân khối lớn |
| `Electric` | Xe điện |

No sixth / “Xem tất cả” category exists in the domain. Homepage previously linked “Xem tất cả xe →” to `/xe` beside the grid — **removed**.

## Targets

| Category | Target published |
|----------|------------------|
| Xe tay ga | 6 |
| Xe số | 4 |
| Xe côn tay | 4 |
| Xe phân khối lớn | 4 |
| Xe điện | 3 |

## Implementation

- `HieuNgaInventoryTargets` + `HieuNgaInventorySeed.EnsureAsync` on startup  
- Seeds only missing / unpublished demo slugs from `DemoCatalogDefinitions` (sized to targets)  
- Local SVG thumbnails only — **no image download / scraper**  
- Homepage category grid: 5 cards only, CMS counts, thumbnail, CTA **Khám phá**, filtered `/xe?category=`  
- Label rename: Scooter → **Xe tay ga**  
- Related inventory stays healthy once each category meets target (≥3–4 peers)

## Live counts

On app start, seed writes `docs/PHASE3_SPRINT_3_6_1_INVENTORY.md` with **before / created / after**.

Restart the web app once to apply inventory ensure against the live DB.

## Files modified / added

- `Domain/MotorcycleCategoryLabels.cs`
- `Application/Catalog/HieuNgaInventoryTargets.cs` (new)
- `Application/DemoImport/DemoCatalogDefinitions.cs`
- `Application/DemoImport/DemoMotorcycleMetadata.cs` (parse `xetayga`)
- `Application/Services/HomepageService.cs`
- `Infrastructure/Persistence/HieuNgaInventorySeed.cs` (new)
- `Infrastructure/Persistence/DbInitializer.cs`
- `Web/Pages/Index.cshtml`
- `tests/HieuNga.Tests/DemoCatalogDefinitionsTests.cs`
- `docs/PHASE3_SPRINT_3_6_1.md` (this file)

## Build / tests

`dotnet build` OK · `dotnet test` **10/10**
