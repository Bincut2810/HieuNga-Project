# Architecture Due Diligence Report

**Project:** HieuNga / Xe Máy Hiếu Nga  
**Phase:** 1 — Full Architecture Audit (read-only)  
**Date:** 2026-07-25  
**Scope:** Entire solution (`Domain`, `Application`, `Infrastructure`, `Web`, `Tests`, deploy assets)

---

## Documents produced

| File | Topic |
|------|-------|
| [01_SYSTEM_OVERVIEW.md](01_SYSTEM_OVERVIEW.md) | Purpose, stack, modules |
| [02_SOLUTION_STRUCTURE.md](02_SOLUTION_STRUCTURE.md) | Projects & dependencies |
| [03_DATABASE.md](03_DATABASE.md) | EF model, ER, migrations |
| [04_API.md](04_API.md) | All HTTP endpoints (Razor + health) |
| [05_BUSINESS_FLOW.md](05_BUSINESS_FLOW.md) | End-to-end flows |
| [06_AUTHORIZATION.md](06_AUTHORIZATION.md) | Identity & access |
| [07_CONFIGURATION.md](07_CONFIGURATION.md) | DI, options, env |
| [08_EXTERNAL_SERVICES.md](08_EXTERNAL_SERVICES.md) | Integrations & absences |
| [09_FOLDER_GUIDE.md](09_FOLDER_GUIDE.md) | Folder map |
| [10_TECHNICAL_DEBT.md](10_TECHNICAL_DEBT.md) | Risks (no fixes) |
| [11_IMPLEMENTATION_GUIDE.md](11_IMPLEMENTATION_GUIDE.md) | How to extend |
| [12_AI_CONTEXT.md](12_AI_CONTEXT.md) | Agent onboarding |

Existing ops docs retained: `ARCHITECTURE.md` (short legacy), `ENVIRONMENT.md`, `DEPLOY-RENDER.md`, `STAGING-CHECKLIST.md`.

**Source code was not modified** in this phase (documentation only).

---

## Scores (architect judgment)

| Dimension | Score | Rationale |
|-----------|------:|-----------|
| **Overall architecture** | **7.0 / 10** | Clear 4-layer skeleton and deployable monolith; weakened by Admin bypassing Application and thin tests. |
| **Maintainability** | **6.5 / 10** | Feature folders help; large seed/Admin model files and duplicated defaults hurt. |
| **Scalability** | **5.0 / 10** | Fine for regional dealership traffic; no cache/queue/read-split; single process. |
| **Complexity** | **5.5 / 10** | Moderate domain breadth; accidental complexity from dual booking models & dual calculators. |
| **Coupling** | **6.0 / 10** | Domain stays clean; Web↔Infrastructure coupling high (typical for small Razor CMS). |

Scale: 10 = excellent for this product class; 1 = unusable.

---

## Strong points

1. Coherent Clean Architecture **project split** with pure Domain.  
2. Real EF migrations + startup migrate/seed suitable for PaaS.  
3. Production-minded pieces already present: health check, forwarded headers, Cloudinary abstraction, env documentation, Docker/Render assets.  
4. Public UX stack (Razor + HTMX + Alpine) fits marketing/lead-gen well.  
5. SEO fields and soft-delete exist on catalog content.  
6. Customer constraints (branding, hide service prices, finance partners) are encoded and documented.

## Major risks

1. **Near-zero automated tests** — regressions likely during feature work.  
2. **Admin vs Application write-path split** — business rules diverge over time.  
3. **Public form spam** — no rate limit/CAPTCHA.  
4. **Soft-delete filter gaps** on lead/banner/settings tables.  
5. **Calculator mismatch** (JS vs server) — trust/support risk.  
6. **Startup seed sync** mutating finance/branding — powerful; must stay idempotent.  
7. **CDN dependency** for core CSS/JS.  
8. **Unused JWT package / unused roles** — false sense of enterprise auth.

## Recommended implementation order (future features)

1. **Test foundation** — booking create, installment calculate, bank sync idempotency, admin auth challenge.  
2. **Lead quality** — CAPTCHA/rate limit + optional notification channel (email) for new leads.  
3. **Application command layer for one Admin vertical** (e.g. motorcycles) as a template for others.  
4. **Align installment math** (single shared formula or clearly labeled estimates).  
5. **Harden soft-delete** filters or explicit Admin queries.  
6. **Observability** — structured logging, request IDs, basic metrics.  
7. **Only then** ERP-like modules (inventory/invoice/warranty) as new bounded contexts.

## Explicit non-goals confirmed by audit

This codebase does **not** currently implement invoice, payment capture, warranty workflows, inventory movements, SMS/email gateways, Redis, or message queues. Planning should not assume hidden implementations.

## Next phase suggestion

Phase 2 should be a **prioritized product backlog** mapped onto these docs (with explicit “new module” vs “extend existing” labels)—still without drive-by refactors unless scheduled.
