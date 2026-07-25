# 11 — Implementation Guide (Future Development)

This guide captures **current conventions** so new work stays consistent. It is not a mandate to refactor old code immediately.

## Architecture principles in use

1. **Monolith Razor Pages** — prefer new UI as Razor Pages under feature folders.
2. **Domain purity** — entities/enums stay framework-free.
3. **Application services for public use cases** — especially reads and lead creation.
4. **Infrastructure owns EF + Identity + third parties**.
5. **Web is composition root** — DI wiring lives in `Program.cs` + `*DependencyInjection.cs`.
6. **PostgreSQL is source of truth** — avoid new static catalogs when DB exists.
7. **Deploy without secrets in git**.

## Where new APIs / endpoints should go

| Need | Put it here |
|------|-------------|
| New public HTML page | `HieuNga.Web/Pages/{Feature}/` with `@page` route |
| HTMX partial | Named handler on same PageModel or shared partial |
| Admin CMS screen | `Pages/Admin/{Feature}/` + authorize via folder convention |
| JSON health/ops endpoint | Minimal API in `Program.cs` sparingly |
| Future REST API | Prefer new `HieuNga.Api` project later — **do not** invent Controllers ad hoc inside Web without team decision |

There are **no** Controllers today; do not silently introduce a second parallel API style.

## Services

| Kind | Location | Rule |
|------|----------|------|
| Public/domain use case | `Application/Services` + interface | Inject repos/UoW; return DTOs |
| Infra-backed catalog/config | Interface in Application, impl in Infrastructure | e.g. finance, site settings, image storage |
| Web-only (cookies, upload glue) | `Web/Services` | No business persistence rules if avoidable |

**Prefer** extending Application services for new public writes.  
**When touching Admin:** either continue existing PageModel+Repository pattern **or** deliberately introduce Application commands — don’t mix both for the same feature without reason.

## Repository rules

- Use `IRepository<T>` / specialized interfaces for aggregate access.
- Soft-delete via `SoftDeleteAsync` for catalog entities with query filters.
- Call `IUnitOfWork.SaveChangesAsync` once per use-case.
- Avoid leaking `IQueryable` to Web.
- For Admin list screens already using `DbContext` AsNoTracking queries, stay consistent within that module.

## DTO rules

- Public UI contracts live in `Application/DTOs`.
- Admin may use page-local `*InputModel` with DataAnnotations (current style).
- Do not return EF entities to Razor when Application DTOs already exist for that flow.
- Keep records/DTOs immutable-friendly where possible.

## Entity rules

- Inherit `BaseEntity`.
- Use Guid IDs.
- Add Fluent configuration in `Persistence/Configurations`.
- Unique business slugs where pages are public.
- Prefer explicit FK delete behaviors (Restrict vs Cascade) matching seed/admin UX.
- JSON text fields: document schema in comments when adding new ones.

## Validation rules

- FluentValidation for shared Application DTOs (extend existing assembly registration).
- DataAnnotations OK on Admin input models.
- Never trust client-only Alpine/HTMX validation.
- Phone regex already used for bookings — reuse patterns for similar leads.

## Dependency rules

```
Web → Application, Infrastructure
Infrastructure → Application, Domain
Application → Domain
Domain → ∅
```

Forbidden:

- Domain referencing EF/ASP.NET.
- Application referencing Web.
- New circular project references.

## Folder & naming conventions

| Item | Convention |
|------|------------|
| Public routes | Vietnamese URL segments (`/xe`, `/bao-duong`) |
| Admin routes | `/admin/...` Vietnamese segments |
| C# namespaces | `HieuNga.*` English |
| Brand text | Customer-facing **Xe Máy Hiếu Nga** |
| PageModels | `*Model` in same folder as `.cshtml` |
| Interfaces | `I{Name}` |
| Options | `{Name}Options` + `SectionName` |

## Coding style currently observed

- File-scoped namespaces.
- Primary constructor DI on services/PageModels.
- Nullable reference types enabled.
- Async end-to-end for I/O.
- Vietnamese UI copy; English code identifiers.

## Front-end conventions

- Public: Tailwind CDN utilities + `site.css` polish.
- Interactivity: Alpine stores / HTMX partials; re-init after swaps (`polish.js`).
- Admin: `admin.css` design system; antiforgery on forms.
- Prefer progressive enhancement; keep calculator results clearly “estimate”.

## Data / seed conventions

- Empty-table seeds for catalogs.
- Idempotent sync only when product explicitly requires deployed DB convergence (banks/branding).
- Never overwrite Admin-customized content on every startup unless guarded.
- Production admin seed must be opt-in.

## Testing expectations (target)

1. Application service unit tests (calculate, booking create mapping).  
2. Critical seed/idempotency tests.  
3. Smoke tests for `/health` and auth challenge on `/admin`.  

Current suite is insufficient — treat new features as needing tests.

## PR / change checklist (recommended)

- [ ] Correct layer for the change  
- [ ] Migration if model changed  
- [ ] Env docs updated if new config keys  
- [ ] Public price policy respected (services)  
- [ ] Branding strings use Xe Máy Hiếu Nga  
- [ ] Build Release green  
