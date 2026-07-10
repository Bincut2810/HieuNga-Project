# Environment configuration

How to configure Honda Hiếu Nga locally and in production without committing secrets.

## Quick reference

| Variable | Purpose | Local dev | Staging / Production |
|----------|---------|-----------|---------------------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` | `Production` |
| `ASPNETCORE_URLS` | Kestrel bind URLs | `http://localhost:5000` | `http://0.0.0.0:8080` (Render sets `PORT`) |
| `ConnectionStrings__DefaultConnection` | PostgreSQL | Local Docker port 5433 | **Required** — Render/Neon + SSL |
| `Site__BaseUrl` | Canonical / OG base URL | `http://localhost:5000` | `https://your-service.onrender.com` |
| `Site__Name` | Site display name | Default in appsettings | **`Xe Máy Hiếu Nga`** on Render |
| `Site__Hotline` | Hotline display | Default | Set on hosting |
| `Site__ZaloUrl` | Zalo deep link | Default | Set on hosting |
| `Site__DefaultSeoTitle` | Fallback `<title>` | Default | Set on hosting |
| `Site__DefaultSeoDescription` | Fallback meta description | Default | Set on hosting |
| `SeedOptions__AdminEmail` | Initial admin email | Dev default | **Required** for first admin |
| `SeedOptions__AdminPassword` | Initial admin password | Dev default | **Required**, 12+ chars |
| `SeedOptions__AdminSeedEnabled` | Allow admin creation | `true` in Development | `true` only on first deploy |
| `SeedOptions__EnableDemoSeed` | Seed demo motorcycles/content | `true` in Development | `true` for one-time demo |
| `SeedOptions__RunContentEnricher` | Overwrite motorcycle demo fields | `true` in Development | **`false`** (default) |
| `ImageStorage__Provider` | `Local` or `Cloudinary` | `Local` | `Cloudinary` recommended |
| `ImageStorage__MaxFileSizeMb` | Max upload size | `5` | `5` |
| `ImageStorage__Cloudinary__*` | Cloudinary credentials | Empty | Set on hosting |

**Aliases:** `AdminSeed__Enabled` → `SeedOptions__AdminSeedEnabled`, `AdminSeed__Email` → `SeedOptions__AdminEmail`, `AdminSeed__Password` → `SeedOptions__AdminPassword`.

Use `__` (double underscore) for nested keys in environment variables.

---

## Local development

### Option A — `appsettings.Development.json`

Default connection string matches `docker/docker-compose.yml` postgres on port **5433**.

```powershell
cd docker
docker compose up postgres -d
cd ../src/HieuNga.Web
dotnet run
```

### Option B — `.env` file

Copy [`.env.example`](../.env.example) to `.env` at the repository root. Load variables before `dotnet run`.

**Never commit `.env`.**

---

## Docker Compose (local full stack)

`docker/docker-compose.yml` uses **local-only** credentials:

- User: `hieunga`
- Password: `hieunga_dev_2025`

**Do not reuse in production or on Render.**

```powershell
cd docker
docker compose up --build
```

- Web: http://localhost:8080
- Health: http://localhost:8080/health

---

## Staging / Production (Render, Neon, Railway)

1. Set `ConnectionStrings__DefaultConnection` (Npgsql format with `SSL Mode=Require` for Render/Neon).
2. Set `Site__BaseUrl` to your public HTTPS URL (required for SEO / Open Graph).
3. Set `Site__Name=Xe Máy Hiếu Nga` (and SEO title/description if not using defaults).
4. For first deploy:
   - `SeedOptions__AdminSeedEnabled=true`
   - `SeedOptions__AdminEmail` + `SeedOptions__AdminPassword` (12+ characters)
   - Optional: `SeedOptions__EnableDemoSeed=true` for demo motorcycles and content
5. After admin login works, set `SeedOptions__AdminSeedEnabled=false`.
6. Leave `SeedOptions__RunContentEnricher` unset or `false`.
7. Configure image storage:
   - `ImageStorage__Provider=Cloudinary` + Cloudinary env vars for persistent uploads
   - Without Cloudinary, use URL-based images in Admin (upload disabled with friendly message)

If production admin env vars are missing or `AdminSeedEnabled` is false, **no default admin is created**.

---

## Admin seed behavior

| Environment | Behavior |
|-------------|----------|
| Development | Creates admin from `appsettings.Development.json` if missing |
| Staging/Production | Creates admin **only** when `AdminSeedEnabled=true` **and** email + password (12+ chars) are set **and** no admin exists yet |
| All | Never overwrites an existing admin password |
| All | Never logs the admin password |

---

## Database migration & seed behavior

On every startup:

1. **Migrations** — `Database.MigrateAsync()` with retry (safe for staging).
2. **Demo motorcycles/banners** — only when DB has no motorcycles **and** (`Development` **or** `EnableDemoSeed=true`).
3. **Admin user** — separate step; see above.
4. **Extra demo content** (promotions, blog) — only when `Development` **or** `EnableDemoSeed=true`.
5. **Service catalog & banks** — only when respective tables are empty (does not overwrite CMS edits).
6. **Site setting defaults** — only adds missing keys (does not overwrite).
7. **Content enricher** — only in Development or when `RunContentEnricher=true`.

---

## Image storage

| Provider | When | Persistence |
|----------|------|-------------|
| `Local` | Development | Files in `wwwroot/uploads/` — **lost on container restart** |
| `Cloudinary` | Staging/Production | Public HTTPS URLs — survives redeploy |

Admin motorcycle form supports file upload (when enabled) or URL input. Gallery images on `/admin/xe/{id}/noi-dung` remain URL-based.

---

## Health check

`GET /health` returns JSON:

```json
{
  "status": "Healthy",
  "database": "Connected",
  "environment": "Production",
  "timestamp": "2026-07-08T04:55:00Z"
}
```

Returns HTTP **503** when the database cannot connect. No connection string or secrets are exposed.

---

## Development-only admin defaults

When `ASPNETCORE_ENVIRONMENT=Development`:

- Email: `admin@hondahieunga.vn`
- Password: in `appsettings.Development.json` (local demo only)

Change for your machine; do not deploy these to production.
