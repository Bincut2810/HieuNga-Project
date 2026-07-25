# Sprint 3.7 — Flagship Homepage Hero Carousel

## 1. Hero audit

| Area | Finding |
|------|---------|
| Markup | Was inline in `Pages/Index.cshtml`; now `Pages/Shared/_HomeHero.cshtml` |
| CMS | `Banner` already had desktop/mobile image, title, subtitle, primary CTA, sort, active, schedule |
| Gaps closed | Secondary CTA, badge, overlay strength, text alignment; schedule fields exposed in admin form |
| JS | Single carousel in `wwwroot/js/homepage.js` (`initHomeHero`); review slider remains separate (not shared) |
| DTO load | `HomepageService` → `GetByPositionAsync(Hero)` unchanged |

## 2. Files modified

- `Domain/Entities/Banner.cs` + `Enums/BannerTextAlignment.cs`
- `Application/DTOs/CommonDtos.cs`, `Mappings/EntityMappers.cs`
- Migration `BannerHeroCarouselFields`
- `Pages/Shared/_HomeHero.cshtml` (new), `Pages/Index.cshtml`
- `wwwroot/css/site.css`, `wwwroot/js/homepage.js`
- Admin `_BannerForm.cshtml`, `ContentModels.cs`
- `HieuNgaHeroBannerSeed.cs`, `DbInitializer.cs`
- Tests: `BannerHeroDtoTests.cs`
- This doc

## 3. CMS fields

| Field | Property | Notes |
|-------|----------|--------|
| Desktop image | `ImageUrl` | Required |
| Mobile image | `MobileImageUrl` | Optional `<picture>` source |
| Title / Subtitle | `Title`, `Subtitle` | |
| Primary CTA | `CtaText`, `CtaUrl` | |
| Secondary CTA | `SecondaryCtaText`, `SecondaryCtaUrl` | Optional; hidden if URL empty |
| Badge | `Badge` | Falls back to brand eyebrow |
| Priority | `SortOrder` | |
| Published | `IsActive` | |
| Schedule | `StartDate`, `EndDate` | Filtered in repo; **UI ready** in admin |
| Overlay | `OverlayStrength` (0–100) | CSS `--hero-overlay` |
| Alignment | `TextAlignment` | Left / Center / Right |

## 4. Performance

- First slide: `fetchpriority="high"`, `loading="eager"`, `<link rel="preload">` in `@section Head`
- Other slides: `loading="lazy"`, `fetchpriority="low"`
- Aspect-ratio track (16:9 mobile / 16:6 desktop) to reduce CLS
- Width/height on `<img>`; no heavy carousel libraries

## 5. Behavior

- Autoplay 6s, infinite loop
- Pause on hover / focus / hidden tab
- Keyboard arrows (+ Home/End)
- Touch swipe (horizontal bias)
- Fade + translate; `prefers-reduced-motion` disables motion + autoplay progress

## 6. Out of scope (untouched)

Installment, motorcycle detail, admin architecture (only banner form fields), service pages, homepage modules below the hero.

## 7. Verify

```bash
dotnet build
dotnet test
# With Postgres up: run web app once so migration + hero seed apply
```
