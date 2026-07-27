# Staging deployment verification checklist

Use this checklist after deploying to Render (or similar) before sharing the demo URL with the client.

**Base URL:** `https://YOUR-SERVICE.onrender.com`  
**Date tested:** _______________  
**Tester:** _______________

---

## Public site

| # | Check | URL / action | Pass? | Notes |
|---|-------|--------------|-------|-------|
| 1 | Homepage loads | `/` | ☐ | Brand: **Xe Máy Hiếu Nga** (not Honda Hiếu Nga) |
| 2 | Motorcycle listing | `/xe` | ☐ | |
| 3 | Motorcycle detail | `/xe/honda-vision-2025` (or any slug) | ☐ | Calculator: HD Bank, MB Bank, JACCS @ **0,79%/tháng** |
| 4 | Motorcycle image visible | Detail page gallery/thumbnail | ☐ | SVG demo or uploaded URL |
| 5 | Maintenance listing | `/bao-duong` | ☐ | No public service prices |
| 6 | Maintenance detail | `/bao-duong/bao-duong-dinh-ky` | ☐ | "Báo giá sau khi kiểm tra" block |
| 7 | Installment page | `/tra-gop` | ☐ | |
| 8 | Contact page | `/lien-he` | ☐ | |
| 9 | Contact form submits | Submit consultation form | ☐ | |
| 10 | Service booking submits | `/bao-duong` booking form | ☐ | |
| 11 | Installment calculator | Detail page or `/tra-gop` | ☐ | Uses DB bank/rate data |
| 12 | Mobile layout | Resize browser or phone | ☐ | Header, CTA bar, cards |
| 13 | Browser console | DevTools → Console | ☐ | No major JS errors |

---

## Admin CMS

| # | Check | URL / action | Pass? | Notes |
|---|-------|--------------|-------|-------|
| 14 | Admin login page | `/admin/dang-nhap` | ☐ | No dev password hint shown |
| 15 | Admin login works | Credentials from env seed | ☐ | |
| 16 | Dashboard | `/admin` | ☐ | |
| 17 | Motorcycle list CRUD | `/admin/xe` → add/edit | ☐ | |
| 18 | Image upload or URL | Add/edit motorcycle thumbnail | ☐ | Cloudinary or URL per config |
| 19 | Service price CRUD | `/admin/dich-vu/bang-gia` | ☐ | |
| 20 | Bank / rate CRUD | `/admin/tra-gop` | ☐ | |
| 21 | Site settings | `/admin/cai-dat` | ☐ | |

---

## Infrastructure & security

| # | Check | Expected | Pass? | Notes |
|---|-------|----------|-------|-------|
| 22 | Health endpoint | `GET /health` | ☐ | `status: Healthy`, `database: Connected` |
| 23 | SEO base URL | View page source `og:image` / canonical | ☐ | Uses `Site__BaseUrl` |
| 24 | HTTPS | Browser padlock | ☐ | Render provides TLS |
| 25 | No stack traces | Trigger 404 `/not-found-test` | ☐ | Generic error page, not developer page |
| 26 | No dev secrets on login | `/admin/dang-nhap` in Production | ☐ | No appsettings hint |
| 27 | Cold start acceptable | First load after idle | ☐ | Free tier: 30–90s |

---

## Environment variables to confirm on hosting

- [ ] `ConnectionStrings__DefaultConnection` (with SSL)
- [ ] `Site__BaseUrl` = public HTTPS URL
- [ ] `Site__Name` = `Xe Máy Hiếu Nga`
- [ ] `SeedOptions__AdminSeedEnabled` = `false` (after first admin login)
- [ ] `ImageStorage__Provider` = `Cloudinary` (if using uploads)
- [ ] Cloudinary credentials set (if using uploads)

---

## Known staging limitations (free tier)

- App sleeps after ~15 minutes idle; first request is slow (cold start).
- Local filesystem uploads are **not** persistent — use Cloudinary or URLs.
- Render free PostgreSQL may expire after 90 days (check Render dashboard).
- Startup does not seed motorcycles; create inventory in Admin CMS.

---

## Sign-off

- [ ] All critical checks passed (items 1–5, 14–16, 22–26)
- [ ] Client demo URL shared: _______________________________
- [ ] Admin credentials delivered securely (not in git)
