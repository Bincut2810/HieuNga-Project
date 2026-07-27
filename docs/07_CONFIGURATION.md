# 07 — Configuration

## Composition root

`src/HieuNga.Web/Program.cs` is the only host entrypoint.

Pipeline (simplified):

1. PORT → ASPNETCORE_URLS rewrite  
2. DI: Application + Infrastructure + Web services  
3. Forwarded headers  
4. Exception handler / HSTS (non-Dev)  
5. Response compression  
6. HTTPS redirection  
7. Static files  
8. Routing → Authentication → Authorization  
9. SeoMiddleware  
10. `/health` + Razor Pages  
11. DbInitializer (fail-fast)  
12. Run  

## Dependency Injection map

### Application (`AddApplication`)

Scoped: Homepage, Motorcycle, Booking, Installment, Promotion, Blog, Branch services + FluentValidation assembly scan.

### Infrastructure (`AddInfrastructure`)

| Registration | Lifetime |
|--------------|----------|
| Options: Seed, Site, ImageStorage | Configure / PostConfigure |
| `HieuNgaDbContext` | Scoped |
| Identity + EF stores | — |
| `IRepository<>`, specialized repos, `IUnitOfWork` | Scoped |
| ServiceCatalog, FinanceConfig, SiteSettings | Scoped |
| Image storage implementations + router | Singleton |

### Web extras

| Registration | Lifetime |
|--------------|----------|
| `CompareSessionService` | Scoped |
| `SiteSettingsPageFilter` | Scoped (+ global MVC filter) |
| Antiforgery, ResponseCompression, HttpContextAccessor | — |

## Configuration sources

Standard ASP.NET Core chain: appsettings → environment-specific → environment variables → (optional secrets).

Nested keys use `__` in env vars (e.g. `ConnectionStrings__DefaultConnection`).

## Options classes

### SiteOptions (`Site`)

Name, BaseUrl, Phone, Hotline, ZaloUrl, DefaultSeoTitle, DefaultSeoDescription.

Used for OG/canonical base URL in layout and defaults.

### ImageStorageOptions (`ImageStorage`)

Provider (`Local`|`Cloudinary`), MaxFileSizeMb, Cloudinary credentials.

### SeedOptions (`SeedOptions`)

AdminEmail, AdminPassword, AdminSeedEnabled.

**Aliases:** `AdminSeed__Enabled|Email|Password` PostConfigured onto SeedOptions.

Motorcycles and CMS content are never seeded at startup (Phase 6).

## Environment files

| File | Intent |
|------|--------|
| `appsettings.json` | Safe defaults; empty connection string |
| `appsettings.Development.json` | Local Postgres; admin seed on; Local images |
| `appsettings.Production.json` | Cloudinary provider; seed off; staging BaseUrl placeholder |

## Runtime environments

| Env | Typical use |
|-----|-------------|
| Development | Local `dotnet run` |
| Production | Docker / Render (also used by local compose web service) |

There is no separate named `Staging` environment; staging uses Production + env vars.

## Logging

Default ASP.NET Core logging via appsettings LogLevel.

Startup logs: DB init success/failure; environment/URLs.

No Serilog/NLog/OpenTelemetry wired.

## Caching

**No** `IMemoryCache` / Redis / response caching middleware configured for business data.

Static files served normally; `asp-append-version` used on some assets.

## Secrets management

| Local | Hosted |
|-------|--------|
| `appsettings.Development.json` may contain local-only DB password | Render env vars / Blueprint `sync: false` |
| `.env` gitignored; `.env.example` placeholders | Never commit real Cloudinary/Admin passwords |

User-secrets folder not required by current docs but compatible with ASP.NET Core patterns.

## Related operational docs

- [ENVIRONMENT.md](ENVIRONMENT.md) — env var reference  
- [DEPLOY-RENDER.md](DEPLOY-RENDER.md) — hosting steps  
- [STAGING-CHECKLIST.md](STAGING-CHECKLIST.md) — verification  
