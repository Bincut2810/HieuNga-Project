# Phase 3 — Finance Calculator (Final)

**Status:** Closed. Self-contained subsystem.  
**Date:** 2026-07-26

This document is the single source of truth for the **motorcycle detail finance calculator**.  
Historical notes that mention `detail-finance.js`, Alpine stores, or `motorcycle.finance.*` SiteSettings are obsolete.

---

## Architecture

```
Motorcycle (+ variants)
        ↓
MotorcyclePricing.ResolveEffectivePrice()
        ↓
FinanceBanks (CMS · IFinanceConfigService.GetActiveBanksAsync)
        ↓
FinanceCalculatorViewModel.Create(price, banks)
        ↓
_DetailFinanceCalculator.cshtml  →  data-* attributes
        ↓
finance-calculator.js  →  FinanceCalculator (vanilla ES module)
```

**Enable rule (automatic):**

`effectivePrice > 0` **and** `banks.Count > 0` → `CalculatorEnabled = true` → section rendered.

No per-bike SiteSettings. No startup ensure. No inventory finance repair. No admin finance prefs. No Alpine store. No polish/HTMX finance boot.

---

## Data flow

| Step | Owner | Responsibility |
|------|--------|----------------|
| Price | `MotorcyclePricing` | First positive variant price, else `BasePrice` |
| Banks | `FinanceConfigService` / CMS `/admin/tra-gop` | Shared list for every motorcycle |
| Defaults | `FinanceMath` | Down 20%, term 12 months; default bank = `IsDefault` else first |
| ViewModel | `FinanceCalculatorViewModel` | `Price`, `Currency`, `Banks`, `DefaultBankId`, `DefaultDownPaymentPercent`, `DefaultTermMonths`, `CalculatorEnabled` |
| Markup | `_DetailFinanceCalculator.cshtml` | SSR + `data-*` for client |
| Client | `wwwroot/js/finance-calculator.js` | Interactive recalculation (mirrors `FinanceMath.Compute`) |

Listing teasers (`ToEstimatedMonthly`) call `FinanceMath.EstimatedMonthly` only — same flat formula, fallback rate `0.0079`.

---

## Initialization flow

1. `_Layout` loads `finance-calculator.js` once as `type="module"`.
2. On `DOMContentLoaded`: if no `[data-finance-calculator]`, exit after registering a single `htmx:afterSwap` listener (so boosted navigations can mount later).
3. If the element exists: create one `FinanceCalculator` per root (`WeakMap` + `data-fc-ready`), bind events once, `calculate()` + `render()`.
4. After HTMX main-content swap: if the swapped subtree contains a calculator root, `initialize(target)` runs. Already-mounted roots are skipped.

No polling. No `MutationObserver`. No custom events. No global Alpine store. No duplicate listeners on the same root.

---

## Calculation flow

**Server & client (flat estimate):**

```
down      = price × (downPercent / 100)
principal = price − down
monthly   = round(principal / term + principal × monthlyRate)
interest  = monthly × term − principal
total     = down + monthly × term
```

Implemented once in C# as `FinanceMath.Compute`.  
Client `compute()` in `finance-calculator.js` must stay identical.

**Out of scope for this module:** `/tra-gop` lead calculator uses `InstallmentService.Calculate` (amortizing). That is a separate lead-capture flow, not the detail estimator.

---

## Public extension points

| Extension | How |
|-----------|-----|
| Change bank rates / partners | Admin → Trả góp (CMS banks). No code change. |
| Change default down / term | `FinanceMath.DefaultDownPaymentPercent` / `DefaultTermMonths` |
| Change price rule | `MotorcyclePricing.ResolveEffectivePrice` only |
| Change formula | `FinanceMath.Compute` + mirror in `finance-calculator.js` |
| Listing teaser format | `PriceExtensions.ToEstimatedMonthly` → `FinanceMath.EstimatedMonthly` |

Do **not** add per-motorcycle finance preferences, startup repair, or Alpine/global state.

---

## File map

| Path | Role |
|------|------|
| `Application/Finance/MotorcyclePricing.cs` | Effective price |
| `Application/Finance/FinanceMath.cs` | Flat math + defaults |
| `Application/Finance/FinanceCalculatorViewModel.cs` | Detail ViewModel |
| `Pages/Xe/ChiTiet.cshtml(.cs)` | Builds ViewModel; renders partial when enabled |
| `Pages/Shared/_DetailFinanceCalculator.cshtml` | UI + data attributes |
| `wwwroot/js/finance-calculator.js` | Client calculator |
| `tests/.../FinanceCalculatorTests.cs` | Unit coverage |

Admin motorcycle editor **Giá** tab manages variants / BasePrice only. Banks stay under Admin Tra góp.

---

## Regression checklist

- [ ] Listing monthly teaser shows for priced bikes
- [ ] Detail calculator appears when price + ≥1 bank
- [ ] Hidden when price is 0 or banks empty
- [ ] Variant / bank / term / down buttons + slider update monthly, interest, total
- [ ] HTMX listing → detail mounts calculator once
- [ ] Browser Back / hard refresh / mobile / desktop — no console errors
