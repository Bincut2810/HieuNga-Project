# 08 — External Services

## Present integrations

| Service | Purpose | Config | Notes |
|---------|---------|--------|-------|
| **PostgreSQL** | Primary datastore | `ConnectionStrings__DefaultConnection` | Required |
| **Cloudinary** | Persistent image upload | `ImageStorage__Cloudinary__*` | Staging/Production recommended |
| **Local filesystem** | Dev image upload | `wwwroot/uploads` | Ephemeral on container hosts |
| **Render / Docker host** | Compute + TLS proxy | PORT, forwarded headers | Not a code SDK |

## Explicitly absent

| Service | Status |
|---------|--------|
| Email (SMTP/SendGrid/MailKit) | Not integrated |
| SMS | Not integrated |
| Redis | Not integrated |
| RabbitMQ / Azure Service Bus / Kafka | Not integrated |
| Firebase / push | Not integrated |
| Payment gateway | Not integrated |
| Bank APIs for loan approval | Not integrated |
| Google Maps API key usage | Map embed URL stored as string only |
| CDN for app assets | Tailwind/Alpine/HTMX loaded from public CDNs |

## Background jobs / cron

| Mechanism | Status |
|-----------|--------|
| `IHostedService` / `BackgroundService` | **None** |
| Hangfire / Quartz | **None** |
| Cron containers | **None** in repo |

Startup seed/migrate runs **inline** during app boot (blocks ready until complete).

## CDN / third-party front-end

Loaded from public CDNs on public layout (and Admin login):

- Tailwind CSS CDN  
- Alpine.js  
- HTMX  
- Google Fonts (Inter)

Admin shell mostly uses local `admin.css` (except login page Tailwind).

## Image storage decision matrix

| Provider | When | Persistence |
|----------|------|-------------|
| Local | Development / fallback | Disk under content root |
| Cloudinary | `Provider=Cloudinary` + credentials | Cloud HTTPS URLs |
| Disabled | Prod Cloudinary selected but missing creds | Upload blocked; URL input still OK |

## Operational dependency checklist for staging

1. PostgreSQL reachable with SSL as required by host.  
2. Optional Cloudinary for Admin uploads.  
3. Public CDN availability (Tailwind/Alpine/HTMX) — offline intranet would break styling/behavior unless vendored later.
