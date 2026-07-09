# Deploy Honda Hiếu Nga lên Render.com (hướng dẫn từng bước)

Dành cho người mới. Làm **đúng thứ tự** bên dưới.

---

## Tổng quan (3 phần)

| Phần | Việc cần làm |
|------|----------------|
| A | Đưa code lên **GitHub** |
| B | Tạo **PostgreSQL** trên Render |
| C | Tạo **Web Service** (Docker) trên Render |

URL demo ví dụ: `https://hieunga-web.onrender.com` (tên service của bạn).

---

## PHẦN A — Đưa project lên GitHub

### A1. Tạo repository trên GitHub (trên web)

1. Mở https://github.com và đăng nhập.
2. Góc phải trên → **+** → **New repository**.
3. **Repository name:** `hieu-nga-showroom` (tên tùy bạn).
4. Chọn **Private** (khuyên dùng cho demo khách hàng).
5. **KHÔNG** tick "Add a README" (project local đã có code).
6. Bấm **Create repository**.
7. Giữ trang mở — sẽ thấy URL dạng:  
   `https://github.com/YOUR_USERNAME/hieu-nga-showroom.git`

### A2. Chạy lệnh trên máy Windows (PowerShell)

Mở PowerShell, chạy **từng dòng** (thay `YOUR_USERNAME` và tên repo):

```powershell
cd d:\HieuNga-Project

git init

git add .

git commit -m "Prepare Honda Hieu Nga showroom for Render deployment"

git branch -M main

git remote add origin https://github.com/YOUR_USERNAME/hieu-nga-showroom.git

git push -u origin main
```

- Lần đầu GitHub hỏi đăng nhập → dùng **Personal Access Token** (không phải mật khẩu GitHub thường).
- Tạo token: GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)** → **Generate new token** → quyền `repo`.

### A3. Kiểm tra

Trên GitHub, refresh repo — phải thấy thư mục `src/`, `Dockerfile`, `docs/`.

---

## PHẦN B — Tạo PostgreSQL trên Render

### B1. Tạo database

1. Vào https://dashboard.render.com
2. **New +** → **PostgreSQL**
3. **Name:** `hieunga-db`
4. **Database:** `hieunga` (mặc định OK)
5. **User:** `hieunga` (mặc định OK)
6. **Region:** **Singapore** (gần Việt Nam hơn)
7. **Plan:** Free (demo) hoặc Starter (ổn định hơn khi demo quan trọng)
8. Bấm **Create Database**
9. Đợi trạng thái **Available** (1–3 phút)

### B2. Lấy connection string

1. Vào database `hieunga-db` vừa tạo.
2. Tab **Connections** hoặc **Info**.
3. Copy **Internal Database URL** (dùng khi Web Service cùng Render)  
   HOẶC **External Database URL** (nếu cần kết nối từ máy local).

Dạng URL:

```
postgresql://hieunga:PASSWORD@dpg-xxxxx-a.singapore-postgres.render.com/hieunga
```

Render Web Service + DB cùng project → dùng **Internal URL** khi link database trong Web Service (Render tự điền).

### B3. Thêm SSL (quan trọng)

Khi dán vào biến môi trường ASP.NET, đổi sang format Npgsql:

```
Host=dpg-xxxxx-a.singapore-postgres.render.com;Port=5432;Database=hieunga;Username=hieunga;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

Render đôi khi có nút **Convert to .NET format** — dùng nếu có.

---

## PHẦN C — Tạo Web Service (Docker)

### C1. Tạo service

1. Dashboard → **New +** → **Web Service**
2. **Connect repository:** chọn GitHub repo `hieu-nga-showroom`
3. Nếu chưa kết nối GitHub: **Connect account** → authorize Render

### C2. Cấu hình build (QUAN TRỌNG — kiểm tra từng ô)

| Ô trên Render | Giá trị |
|---------------|---------|
| **Name** | `hieunga-web` (URL sẽ là `https://hieunga-web.onrender.com`) |
| **Region** | Singapore |
| **Branch** | `main` |
| **Runtime** | **Docker** |
| **Dockerfile Path** | `Dockerfile` |
| **Docker Context** | `.` (dấu chấm = thư mục gốc repo) |
| **Instance Type** | Free hoặc Starter |

### C3. Environment Variables (bấm **Add Environment Variable**)

Thêm **từng dòng** sau:

| Key | Value |
|-----|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://0.0.0.0:8080` |
| `Site__BaseUrl` | `https://hieunga-web.onrender.com` (đổi theo URL thật) |
| `Site__Name` | `Honda Hiếu Nga Đà Nẵng` |
| `Site__Hotline` | `0905 123 456` |
| `Site__ZaloUrl` | `https://zalo.me/0905123456` |
| `Site__DefaultSeoTitle` | `Honda Hiếu Nga Đà Nẵng \| Mua xe & dịch vụ HEAD` |
| `Site__DefaultSeoDescription` | `Đại lý Honda HEAD chính hãng tại Đà Nẵng.` |
| `SeedOptions__EnableDemoSeed` | `true` (demo lần đầu; đặt `false` sau khi ổn định) |
| `SeedOptions__AdminSeedEnabled` | `true` (chỉ lần deploy đầu) |
| `SeedOptions__AdminEmail` | email admin của bạn |
| `SeedOptions__AdminPassword` | mật khẩu mạnh 12+ ký tự |
| `ImageStorage__Provider` | `Cloudinary` (khuyên dùng) hoặc để URL ảnh |
| `ImageStorage__Cloudinary__CloudName` | từ Cloudinary dashboard |
| `ImageStorage__Cloudinary__ApiKey` | từ Cloudinary dashboard |
| `ImageStorage__Cloudinary__ApiSecret` | từ Cloudinary dashboard |

**Connection string** — cách dễ nhất:

1. Bấm **Add from Database** (hoặc **Link Database**)
2. Chọn `hieunga-db`
3. Key tự tạo: `ConnectionStrings__DefaultConnection`  
   (hai dấu gạch dưới `__` — đúng chuẩn ASP.NET Core)

Nếu nhập tay, Key phải là:

```
ConnectionStrings__DefaultConnection
```

(Không phải `ConnectionStrings:DefaultConnection` trên Render — dùng `__`)

### C4. Health check

- **Health Check Path:** `/health`
- Render gọi URL này để biết app còn sống. Response mẫu:

```json
{"status":"Healthy","database":"Connected","environment":"Production","timestamp":"..."}
```

HTTP 503 khi DB không kết nối được.

### C5. Deploy

1. Bấm **Create Web Service**
2. Tab **Logs** mở ra — đợi build Docker (5–15 phút lần đầu)
3. Tìm dòng: `Database initialization completed.`
4. Tìm dòng: `Now listening on` hoặc `Application started`
5. Trạng thái chuyển **Live** → mở URL trên đầu trang

---

## Biến môi trường mẫu (copy tham khảo)

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
ConnectionStrings__DefaultConnection=Host=YOUR_HOST;Port=5432;Database=hieunga;Username=hieunga;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

**Lưu ý:** `PORT` do Render tự inject — app đã đọc `PORT` trong `Program.cs`, không cần thêm tay.

---

## Sau khi deploy — Kiểm tra demo

Mở trình duyệt (tab ẩn danh):

| URL | Kỳ vọng |
|-----|---------|
| `https://YOUR-SERVICE.onrender.com/health` | JSON `{"status":"Healthy","database":"Connected",...}` |
| `https://YOUR-SERVICE.onrender.com/` | Trang chủ Honda Hiếu Nga |
| `https://YOUR-SERVICE.onrender.com/xe` | Danh sách xe |
| `https://YOUR-SERVICE.onrender.com/xe/honda-vision-2025` | Chi tiết xe + trả góp |
| `https://YOUR-SERVICE.onrender.com/admin/dang-nhap` | Admin login |

**Admin (production) — lần deploy đầu:**

Set trên Render → Environment:

```
SeedOptions__AdminSeedEnabled=true
SeedOptions__AdminEmail=your-admin@yourdomain.vn
SeedOptions__AdminPassword=YOUR_STRONG_PASSWORD_MIN_12_CHARS
```

Sau khi đăng nhập admin thành công, đặt `SeedOptions__AdminSeedEnabled=false`.

Không có biến này → **không tạo admin mặc định** (an toàn hơn mật khẩu cố định).

**Ảnh upload (khuyên dùng Cloudinary):**

1. Tạo tài khoản miễn phí tại https://cloudinary.com
2. Lấy Cloud Name, API Key, API Secret
3. Set `ImageStorage__Provider=Cloudinary` và 3 biến `ImageStorage__Cloudinary__*`
4. Không cấu hình Cloudinary → Admin vẫn dùng **URL ảnh**; upload file bị tắt với thông báo rõ ràng

**Checklist đầy đủ:** [STAGING-CHECKLIST.md](STAGING-CHECKLIST.md)

---

## Xem log khi lỗi

1. Render Dashboard → Web Service `hieunga-web`
2. Tab **Logs**
3. Bật **Live tail**
4. Lỗi thường gặp:
   - `Database initialization failed` → sai connection string / chưa SSL
   - `Build failed` → mở log phía trên, tìm dòng `error CS`
   - `Port already in use` → kiểm tra `ASPNETCORE_URLS` và `PORT`

---

## Free tier — Lưu ý demo khách

- App **ngủ** sau ~15 phút không truy cập.
- Lần mở đầu **cold start** 30–90 giây — báo trước với khách.
- Trước demo 5 phút: mở URL, đợi load xong, rồi mới chia sẻ màn hình.

---

## Deploy lại sau khi sửa code

```powershell
cd d:\HieuNga-Project
git add .
git commit -m "Describe your change"
git push
```

Render tự build lại (Auto-Deploy bật mặc định).

---

## Test Docker trên máy trước khi push (tùy chọn)

```powershell
cd d:\HieuNga-Project\docker
docker compose up --build
```

Mở http://localhost:8080 và http://localhost:8080/health

---

## Cấu trúc file deploy trong repo

```
HieuNga-Project/
├── Dockerfile              ← Render build file (gốc repo)
├── render.yaml             ← Blueprint (tùy chọn)
├── .dockerignore
├── .env.example
├── docs/DEPLOY-RENDER.md   ← File này
├── docs/ENVIRONMENT.md     ← Biến môi trường đầy đủ
├── docs/STAGING-CHECKLIST.md
└── src/HieuNga.Web/
    ├── Program.cs          ← PORT, HTTPS proxy, /health
    └── appsettings.Production.json
```

---

## Hỗ trợ nhanh

| Triệu chứng | Cách xử lý |
|-------------|------------|
| 502 Bad Gateway | Xem Logs — app crash khi migrate DB |
| Trang trắng | Đợi cold start; refresh sau 1 phút |
| Ảnh/CSS mất | Kiểm tra `wwwroot/` có trong repo; dùng Cloudinary cho upload Admin |
| Upload ảnh mất sau redeploy | Dùng `ImageStorage__Provider=Cloudinary` hoặc URL ảnh |
| DB connection | Dùng Internal URL + link database trong Render |
