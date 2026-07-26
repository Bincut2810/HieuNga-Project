# Demo Motorcycle Import System

Populate the Motorcycle CMS quickly for dealership demos. Images upload through the **existing** `IImageStorageService` (Cloudinary in Production, Local in Development). No scraping, no remote image downloads.

## Seed Full Catalog (Phase 3.3.1)

Admin → Import Demo Data → **Seed Full Catalog**

Creates **25** presentation motorcycles (`demo-*` slugs), ~5 per category:

| Category | Count |
|----------|------:|
| Scooter | 5 |
| Xe số | 5 |
| Xe côn tay | 5 |
| Xe phân khối lớn | 5 |
| Xe điện | 5 |

Media comes from `DemoAssets/_Shared/` (copied from Vision placeholders if missing):

- thumbnail, gallery (4), colors (3), features/tech
- optional `angles/front.jpg` … `front-right.jpg` (six named angles; skipped unless ≥6 distinct files)

Shared images are uploaded **once**, then URL-reused across catalog bikes (fast, layouts filled). Replace per-bike media later in the Motorcycle Editor.

Idempotent: re-running updates existing `demo-*` rows and refreshes child media/content.

---

## Folder structure

Runtime root (published with the web app):

```
src/HieuNga.Web/DemoAssets/
  Vision/          ← complete sample package
  Lead/            ← stubs (add metadata.json to enable)
  AirBlade/
  SH/
  WinnerX/
  Future/
  WaveAlpha/
```

Documentation mirror (same layout):

```
docs/DemoAssets/
```

### Per-package layout

```
Vision/
  metadata.json          ← required
  thumbnail.jpg
  gallery/
    01.jpg
    02.jpg
    …
  angles/
    front.jpg
    front-left.jpg
    left.jpg
    rear.jpg
    right.jpg
    front-right.jpg      ← need ≥ 2 angles for public viewer
  colors/
    black.jpg
    white.jpg
    red.jpg
  features/              ← optional; referenced from metadata
  technology/            ← optional
  README.md
```

Legacy `360/` folders are still accepted if files are named by angle key (or mapped in order). Supported extensions: `.jpg` `.jpeg` `.png` `.webp` `.gif` `.svg`

## Image naming

| Role | Convention |
|------|------------|
| Thumbnail | `thumbnail.jpg` (or name in `assets.thumbnail`) |
| Gallery | Sorted alphanumeric files in `gallery/` |
| Angles | Named files in `angles/` (`front`, `front-left`, `left`, `rear`, `right`, `front-right`) |
| Colors | File name from metadata `colors[].image`, else `{slugified-name}.jpg` |
| Features / tech | File name from card `image` field under `features/` or `technology/` |

**Do not ship copyrighted Honda product photos.** The Vision package uses 1×1 placeholder JPEGs. Replace files in place, keep names, then **Reimport**.

## metadata.json

Importer reads only this file for text/prices/structure (not hardcoded bike data).

Key fields:

- `name`, `slug`, `category` (`Scooter` \| `XeSo` \| `ConTay` \| `PhanKhoiLon` \| `Electric`)
- `price`, `featured`, `published`, `sortOrder`
- `shortDescription`, `descriptionHtml`
- `engineCc`, `fuelType`, `transmission`
- `highlights[]`, `specifications[]` (`icon`, `label`, `value`; `icon: "group"` = section header)
- `variants[]`, `colors[]`, `features[]`, `technology[]`
- `seo`, `finance`, `assets` (folder/file hints; `spinFolder` defaults to `angles`)

Slug is the **idempotency key**. Reimport updates the same motorcycle and replaces child assets.

## Import process

1. Admin → **Inventory** → **Import Demo Data** (`/admin/xe/import-demo`)
2. Package cards show Ready / Imported / missing metadata
3. **Import** or **Reimport**
4. Service reads `metadata.json`, uploads images via `IImageStorageService`, upserts entities in a DB transaction. Detail finance calculator needs no per-bike prefs — only a valid price + CMS banks (see PHASE3_FINANCE_FINAL.md).
5. Success toast → redirect to motorcycle editor

**Delete Demo** soft-deletes the motorcycle (`IsDeleted`, unpublished).

## Cloudinary behavior

| Environment | Behavior |
|-------------|----------|
| Development | Local disk `wwwroot/uploads/...` if Cloudinary not configured |
| Production (Render) | Cloudinary when `ImageStorage__Cloudinary__*` is set |
| Production without Cloudinary | Import blocked with clear error |

Folders used: `demo/{motorcycleId}/thumb|gallery|colors|angles|features|technology`

No duplicate upload helpers — same `IImageStorageService.UploadAsync` as the CMS Media tab.

## How to add a new motorcycle package

1. Create `DemoAssets/YourBike/` with the folder layout above.
2. Write `metadata.json` (copy Vision’s file and edit).
3. Drop images (placeholders first is fine).
4. Register the package in `DemoPackageCatalog.All` (`HieuNga.Application/DemoImport/DemoMotorcycleMetadata.cs`) if it is not already listed.
5. Deploy / restart → open Import Demo Data → **Import**.

Stub folders (Lead, SH, …) already appear in Admin; they stay disabled until `metadata.json` exists.

## Safety

- Running Import twice does **not** create duplicate slugs.
- Reimport replaces variants, colors, gallery, angles, features, technologies.
- Missing image files produce warnings; the motorcycle is still created when possible.
