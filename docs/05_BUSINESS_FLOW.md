# 05 — Business Flows

> Documented flows found in the codebase.  
> Modules that do **not** exist (invoice, payment gateway, warranty engine, inventory ledger, notifications) are called out explicitly.

## Conventions

Sequence style: **Actor → Web → Service/DB → Result**.

---

## 1. Admin login

```
Admin → GET /admin/dang-nhap
Admin → POST email/password
     → SignInManager.PasswordSignInAsync
     → Cookie issued (8h sliding)
     → Redirect /admin
```

Failure: show error message; no lockout enforced in handler (`lockoutOnFailure: false`).

Seed path (startup, not UI): if Development or `AdminSeedEnabled` + strong password → create admin if missing. Never overwrite existing password.

---

## 2. Homepage render

```
Visitor → GET /
       → IHomepageService.GetHomepageDataAsync
       → Load banners, featured bikes, promos, branches, testimonials
       → Razor Index.cshtml
```

Site settings (hotline, Zalo, SEO defaults) injected by `SiteSettingsPageFilter`.

---

## 3. Browse / search motorcycles

```
Visitor → GET /xe (?q, category, price, page)
       → IMotorcycleService.SearchAsync
       → IMotorcycleRepository.SearchAsync
       → HTML grid
HTMX Filter handler → same service → partial grid
```

---

## 4. Motorcycle detail + installment estimate

```
Visitor → GET /xe/{slug}
       → MotorcyclePricing.ResolveEffectivePrice
       → FinanceBanks (CMS) → FinanceCalculatorViewModel
       → _DetailFinanceCalculator + finance-calculator.js (vanilla)
```

**Canonical doc:** [PHASE3_FINANCE_FINAL.md](./PHASE3_FINANCE_FINAL.md)

**Note:** `/tra-gop` lead flow still uses `InstallmentService.Calculate` (amortizing) — separate from the detail flat estimator.

---

## 5. Compare motorcycles

```
Visitor → Add bike (handler Add)
       → CompareSessionService cookie `honda_compare` (max 3 GUIDs, 7 days)
Visitor → /so-sanh
       → Load bikes by IDs → comparison table
```

No DB persistence for compare lists.

---

## 6. Create test-ride booking (“create customer lead”)

```
Visitor → /dat-lich-lai-thu POST
       → (Page validation) → IBookingService.CreateTestRideBookingAsync
       → Insert Booking { Type=TestRide, Status=Pending }
Admin → /admin/khach-hang/lich-hen/{id}
     → Update Status / AdminNotes
```

No email/SMS confirmation to customer.

---

## 7. Consultation / contact

```
Visitor → /lien-he POST
       → CreateConsultationAsync
       → Insert Booking { Type=Consultation, Notes="[Subject] Message" }
```

---

## 8. Maintenance booking (“repair request” lite)

```
Visitor → /bao-duong (catalog from ServiceItems)
       → POST booking form
       → CreateMaintenanceBookingAsync
       → Insert MaintenanceBooking (MotorcycleModel text, ServiceType text)
Admin → /admin/khach-hang/bao-duong/{id} → status/notes
```

Public pages **hide prices**; Admin keeps internal `EstimatedPriceText`.

There is **no** workshop job card, parts consumption, or technician assignment.

---

## 9. Create / edit vehicle (Admin CMS)

```
Admin → /admin/xe/them POST
     → Validate MotorcycleInputModel
     → Optional IImageStorageService upload → ThumbnailUrl
     → IRepository<Motorcycle>.AddAsync
Admin → /admin/xe/{id}/gia → manage variants (price/stock fields)
Admin → /admin/xe/{id}/noi-dung → JSON highlights/specs + MediaAsset URL lines
Admin → soft delete via /admin/xe/xoa
```

StockQuantity exists on variants but there is **no inventory transaction workflow**.

---

## 10. Content publishing

Flows are standard Admin CRUD for Banner / Promotion / Blog / Branch → EF entities → public list/detail pages filtered by active/published flags.

---

## 11. Service catalog management

```
Admin → categories + bang-gia CRUD
     → ServiceCategory / ServiceItem
Public → read-only catalog (no prices shown)
```

---

## 12. Finance partner configuration

```
Startup → SyncFinancePartnersAsync (HD Bank, MB Bank, JACCS @ 0.79%/month typical)
Admin → /admin/tra-gop/ngan-hang & lai-suat CRUD
Public calculator → GetActiveBanksAsync
```

---

## 13. Installment lead (data model)

```
IInstallmentService.SubmitRequestAsync
  → Calculate → Insert InstallmentRequest
Admin inbox → /admin/khach-hang/tra-gop
```

Confirm UI entry points when extending; calculator pages may not always call `SubmitRequestAsync`.

---

## 14. Site settings

```
Admin → /admin/cai-dat → ISiteSettingsService.UpdateAsync
      → Upsert site_settings keys
Every page → SiteSettingsPageFilter loads DTO into ViewData
```

Also influenced by `Site__*` env vars / `SiteOptions` for BaseUrl & defaults.

---

## 15. Image upload

```
Admin motorcycle form → file
  → ImageStorageRouter
     → Local (dev) | Cloudinary (staging/prod when configured) | Disabled
  → Public URL stored on Motorcycle.ThumbnailUrl
```

Gallery media on NoiDung is URL-line based (no multi-file Cloudinary gallery yet).

---

## Flows that do **not** exist

| Topic | Status |
|-------|--------|
| Invoice / billing | Not implemented |
| Online payment | Not implemented |
| Warranty claims | Not implemented |
| Inventory receive/issue | Not implemented (variant stock field only) |
| Email / SMS / push notifications | Not implemented |
| Role-based multi-admin permissions | Identity roles table exists; unused in policies |
| Customer self-service accounts | Not implemented |

Treat any future work in those areas as **greenfield modules**, not extensions of hidden existing code.
