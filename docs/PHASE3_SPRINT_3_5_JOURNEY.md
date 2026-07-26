# Sprint 3.5 — Customer Journey & Lead Conversion

**Date:** 2026-07-25  
**Scope:** Public journey wiring. Homepage/Detail not redesigned. Installment **formula** unchanged. No DB migration.

---

## 1. Audit summary

| Issue | Finding |
|-------|---------|
| Intent params | Emitted from detail/home/footer but **ignored** by `/lien-he` |
| Hardcoded phones | `_CtaBanner`, LienHe, BaoDuong, `_DetailFinancingResult` used `0905…` |
| Test-ride mis-route | Detail finance “Đặt lái thử” → LienHe with slug, not `/dat-lich-lai-thu?xeId=` |
| Installment leads | `SubmitRequestAsync` existed but **no public submit** |
| Sticky CTA | One-size-fits-all; Zalo could be empty href |
| Attribution | No source tracking; Notes already available |

---

## 2. Problems fixed

1. Universal CTA partial (`_UniversalCta`) — Site Settings only  
2. Intent routing on Contact (`intent`, `xe`, `service`, `source`)  
3. Vehicle inquiry card on Contact / Test ride / Tra góp  
4. Premium test-ride booking (branch, date, time, vehicle, confirmation)  
5. Installment inquiry form → `InstallmentRequest`  
6. Context-aware sticky mobile CTA  
7. Lead attribution in Notes (`[lead source=…]`) — Admin shows “Nguồn lead”  
8. Shared success + trust components  
9. Removed hardcoded phone CTAs  

---

## 3. New customer journey

```
Home / Listing / Detail / Promo / Service / News
        ↓ (intent + xe + source)
   /lien-he  → Consultation Booking (+ MotorcycleId)
   /dat-lich-lai-thu → TestRide Booking
   /tra-gop → Calculate (unchanged) + Inquiry submit
        ↓
   Success card → Call / Zalo / Maps / SLA
        ↓
   Admin LichHen / TraGop shows Notes + Nguồn lead
```

---

## 4. Files modified (key)

**New:** `LeadAttribution.cs`, `_UniversalCta`, `_LeadSuccess`, `_LeadTrust`, `_LeadVehicleCard`  
**Pages:** `LienHe/*`, `DatLichLaiThu/*`, `TraGop/*`  
**Shared:** `_CtaBanner`, `_MobileCta`, `_DetailFinanceCalculator`, `_HomeFeaturedCard`  
**Services:** `BookingService` (MotorcycleId + attribution)  
**Admin (display only):** LichHen/TraGop ChiTiet — Nguồn lead  
**CSS:** lead journey styles  
**Doc:** this file  

---

## 5. Performance

- No new global JS libraries  
- Contact/test-ride/tra-gop use server forms + existing Alpine/HTMX on TraGop calculator only  
- Replaced Unsplash CTA banner image with CSS gradient  
- Detail JS still page-scoped  

---

## 6–7. Build & tests

- Build: succeeded  
- Tests: 10/10 passed  
