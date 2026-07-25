# Phase 4 — Sprint 4.2 Contact / Branch Polish

**Date:** 2026-07-25  
**Scope:** Public branch/contact surfaces + empty/placeholder seed only.  
**Out of scope:** DB schema, lead flow logic, CMS architecture.

---

## 1. Audit

| Location | Before | After |
|----------|--------|-------|
| Seed `DbInitializer` | Single fake branch `123 Nguyễn Văn Linh` | Two HEAD showrooms from catalog |
| Site settings defaults | Placeholder phone/address | Primary HEAD (392 Hoàng Diệu / 0236 384 9556) |
| Homepage branches | One card + mini list / "HEAD Office" | Responsive `_BranchCards` two-card + map |
| Footer | Single `site.Address` | Both CMS branches |
| Liên hệ | One head card + mini list | `_BranchCards` |
| Detail purchase | No branch cards | `_BranchCards` under purchase panel |
| Test ride / Service | Hours/select only | Branch cards + existing selects |
| Lead success / trust | Site address placeholder risk | Cards / neutral copy |
| Config (appsettings, render, `.env.example`) | `0905 123 456` | Real primary hotline |

**Removed strings:** `123 Nguyễn Văn Linh`, `0905 123 456`, `HEAD Office` (UI label → `HEAD` badge).

---

## 2. CMS-first seeding

`HieuNgaBranchSeed.EnsureAsync` runs on startup:

- Inserts missing `head-hieu-nga-1` / `head-hieu-nga-2` only
- Converts known placeholders in place (never duplicates)
- Fills **empty/placeholder** site contact settings only
- Does **not** overwrite intentional CMS edits

Canonical catalog: `HieuNga.Application/Catalog/HieuNgaShowrooms.cs`

| Branch | Address | Phone | Maps |
|--------|---------|-------|------|
| HEAD Hiếu Nga 1 | 392 Hoàng Diệu, Hải Châu, Đà Nẵng | 0236 384 9556 | [maps](https://maps.google.com/?q=392+Hoàng+Diệu+Đà+Nẵng) |
| HEAD Hiếu Nga 2 | 170 Hùng Vương, Hải Châu, Đà Nẵng | 0236 384 9551 | [maps](https://maps.google.com/?q=170+Hùng+Vương+Đà+Nẵng) |

Hours: **07:30–19:00**

---

## 3. UI

Shared partial `_BranchCards.cshtml`: HEAD badge, name, address, phone, hours, actions (Google Maps / Call / Book appointment). Responsive 1→2 columns.

---

## 4. Files modified

- `Application/Catalog/HieuNgaShowrooms.cs` (new)
- `Application/DTOs/CommonDtos.cs` (+ `Slug` on `BranchDto`)
- `Application/Mappings/EntityMappers.cs`
- `Application/Options/SiteOptions.cs`
- `Infrastructure/Persistence/HieuNgaBranchSeed.cs` (new)
- `Infrastructure/Persistence/DbInitializer.cs`
- `Infrastructure/Persistence/ServiceFinanceSeed.cs`
- `Infrastructure/Services/ServiceFinanceServices.cs`
- `Infrastructure/Repositories/SpecializedRepositories.cs` (`!IsDeleted`)
- `Web/Filters/SiteSettingsPageFilter.cs` (loads branches)
- `Web/Pages/Shared/_BranchCards.cshtml` (new)
- `Web/Pages/Shared/_Footer.cshtml`, `_LeadSuccess.cshtml`, `_LeadTrust.cshtml`
- `Web/Pages/Index.cshtml`, `LienHe/Index.cshtml`, `Xe/ChiTiet.cshtml`, `DatLichLaiThu/Index.cshtml`, `BaoDuong/Index.cshtml`
- `wwwroot/css/site.css`
- `appsettings*.json`, `render.yaml`, `.env.example`, `docs/DEPLOY-RENDER.md`
- `docs/PHASE4_SPRINT_4_2_BRANCHES.md` (this file)

---

## 5. Remaining placeholders

None in runtime code for `123 Nguyễn Văn Linh` / `0905 123 456` / `HEAD Office`.  
Detector strings remain only inside `HieuNgaShowrooms` / seed to recognize and replace old demo data.

Zalo URL uses primary landline digits (`zalo.me/02363849556`) as a best-effort placeholder until a real Zalo OA is configured in CMS.

---

## 6. Build / tests

**Verified (2026-07-25):** `dotnet build` OK · `dotnet test` **10/10** passed.