# 🎓 ARS Platform - Backend Web API

**ARS Platform (Academic Research Sharing Platform)** là hệ thống quản lý, xuất bản bài báo khoa học và tổ chức hội thảo học thuật trực tuyến đa vai trò (Multi-Role RBAC), được xây dựng trên nền tảng **ASP.NET Core 8 Web API** và **Entity Framework Core 8**.

---

## 🛠️ Công Nghệ Sử Dụng (Tech Stack)

| Thành phần | Công nghệ / Thư viện | Mô tả |
| :--- | :--- | :--- |
| **Framework** | **.NET 8 (C# 12)** | Nền tảng backend hiệu năng cao |
| **Database** | **SQL Server 2019+** | Cơ sở dữ liệu quan hệ chính |
| **ORM** | **Entity Framework Core 8** | Code-First Migrations, LINQ, Repository Pattern |
| **Xác thực & Phân quyền** | **JWT Bearer + Refresh Token** | Multi-Role RBAC (*Admin, Researcher, Lecturer, Reviewer, Graduate Student*) |
| **Trí tuệ nhân tạo (AI)** | **Google Gemini AI (gemini-3.7-flash)** | Tóm tắt biên bản hội thảo, phân tích âm thanh & tổng hợp khảo sát tự động |
| **Cổng thanh toán** | **PayOS Payment Gateway** | Thanh toán gói phí thường niên trực tuyến, xác thực chữ ký số Webhook |
| **Tích hợp Hội thảo** | **Google Meet & Calendar API** | Tự động tạo link phòng họp Google Meet và đồng bộ lịch hẹn |
| **Định danh học thuật** | **ORCID OAuth 2.0, OpenAlex, Semantic Scholar** | Liên kết tác giả, đồng bộ chỉ số H-index, Citations và số lượng bài báo |
| **Thời gian thực** | **ASP.NET Core SignalR** | Bắn thông báo thời gian thực, trao giải huy hiệu thành tích (*Medal Celebration*) |
| **Email Service** | **SMTP (Gmail / SendGrid)** | Gửi thư mời hội thảo, nhắc hẹn, mã OTP xác thực và thông báo kết quả |
| **Tài liệu API** | **Swagger / OpenAPI (Swashbuckle)** | Tự động sinh tài liệu và giao diện test API |

---

## 🏗️ Cấu Trúc Dự Án (Architecture)

Dự án áp dụng mô hình kiến trúc phân lớp chuẩn (**N-Tier / Clean Architecture**):

```
ARSPlatform/
├── ARSPlatform.API/            # Presentation Layer: Controllers, Middlewares, SignalR Hubs, Program.cs
│   ├── CONTROLLER/             # API Endpoints (Admin, Paper, Seminar, User, Medal, Payment...)
│   ├── Hubs/                   # SignalR Hubs cho thông báo & realtime events
│   ├── Middleware/             # Error Handling, Auth & Logging Middleware
│   ├── appsettings.json        # Cấu hình ConnectionString, JWT, API Keys, PayOS, Gemini
│   └── Program.cs              # DI Container, Pipeline & Middleware Configuration
│
├── ARSPlatform.SERVICE/        # Business Logic Layer: DTOs, Services, AI & Payment Integrations
│   ├── DTOs/                   # Request & Response Data Transfer Objects
│   ├── Interfaces/             # Service Interface contracts
│   ├── ExternalServices/       # Gemini AI, Google Meet, PayOS, OpenAlex, Semantic Scholar, Email
│   └── [Service Implementations] # PaperService, SeminarService, MedalService, UserService...
│
├── ARSPlatform.REPO/           # Data Access Layer: Generic Repository, Unit of Work, Queries
│   ├── Interfaces/             # Repository interfaces
│   └── PAGINATION/             # Pagination helper classes & PagedResult<T>
│
├── ARSPlatform.MODEL/          # Core Domain Layer: Entities, DbContext, Migrations
│   ├── Entities/               # Database Entities (User, Role, Paper, Seminar, Medal, Transaction...)
│   ├── Migrations/             # EF Core Database Migrations
│   └── AppDbContext.cs         # EF Core Database Context & Fluent API Configurations
│
├── ARSPlatform.Tests/          # Unit Tests & Integration Tests (xUnit)
├── Dockerfile                  # Docker container build script
└── ARSPlatform.sln             # Visual Studio Solution
```

---

## 🚀 Tính Năng Nổi Bật Của Backend (Key Modules)

### 1. 👥 Quản lý Người dùng & Xác thực Đa vai trò (Identity & Multi-Role RBAC)
* Xác thực JWT kết hợp Refresh Token an toàn, tích hợp Google OAuth 2.0.
* Luồng gửi duyệt minh chứng vai trò (`RoleRequest`) kèm file PDF, kiểm tra liên kết ORCID / OpenAlex.
* Quản lý tài khoản: Tìm kiếm, lọc theo vai trò, Khóa tài khoản (`Suspend`) và Mở khóa (`Unsuspend`).

### 2. 📄 Quy trình Xuất bản & Bình duyệt Bài báo Khoa học (Peer-Review Workflow)
* **Nộp bài:** Tác giả gửi bản thảo kèm Abstract, PDF, Chuyên ngành (`SubField`), từ khóa, chỉ số ISSN, Open Access.
* **Phân công phản biện:** Admin phân công Reviewer kèm hạn chót (`Deadline`), thù lao (`Fee`), hỗ trợ thuật toán gợi ý chuyên gia theo H-index.
* **Đánh giá Rubric 5 tiêu chí chuẩn hóa:** Chấm điểm định lượng và nhận xét cho từng phần:
  1. *Tính nguyên bản (Originality)*
  2. *Tổng quan tài liệu (Literature Review)*
  3. *Phương pháp nghiên cứu (Methodology)*
  4. *Kết quả & Thảo luận (Results & Discussion)*
  5. *Quy chuẩn trình bày (Formatting)*
* **Quyết định xuất bản:** Admin tổng hợp điểm, ra quyết định *Accept / Reject / Publish* lên kho bài báo công khai.

### 3. 🎥 Hội thảo Khoa học & Tích hợp Gemini AI (Seminars & Conferences)
* Tự động tạo link **Google Meet** và gửi thư mời tham gia qua Email.
* Gửi email nhắc hẹn tự động trước giờ diễn ra sự kiện (`EventReminders`).
* Cấu hình form khảo sát / feedback động cho người tham dự.
* **Gemini AI:** Tự động tóm tắt nội dung file ghi âm (`/summarize-audio`) và tổng hợp đánh giá của người tham dự (`/summarize-feedback`).
* API cập nhật nhanh trạng thái và kết thúc sớm hội thảo (`PUT/PATCH /api/Seminar/{id}/status`, `POST /api/Seminar/{id}/complete`).

### 4. 📚 Nhóm Nghiên cứu & Báo cáo Giai đoạn (Research Groups & Milestones)
* Giảng viên tạo đề tài nghiên cứu (`ResearchTopics`) và nhóm nghiên cứu (`ResearchGroups`).
* Sinh viên gửi yêu cầu tham gia (`JoinRequests`), nộp báo cáo tiến độ theo mốc giai đoạn (`PhasedReports`).
* Giảng viên chấm điểm năng lực (`CapacityEvaluation`), lưu nhận xét phản hồi (`LectureFeedback`).
* Kho tài liệu học tập (`LearningMaterials`) và chia sẻ học liệu chuyên sâu (`SharedMaterials`).

### 5. 🏅 Hệ thống Huy hiệu Học thuật (Academic Medals & Flairs)
* Quản lý danh mục huy hiệu với 4 cấp bậc: *Bronze, Silver, Gold, Platinum*.
* Tự động tính toán tiến độ đạt huy hiệu theo các chỉ số cống hiến thực tế.
* Bắn thông báo real-time qua SignalR khi người dùng mở khóa huy hiệu mới.

### 6. 💳 Quản lý Thuê bao & Thanh toán Trực tuyến (Finance & PayOS)
* Cấu hình gói phí thường niên (`AnnualFees`) cho Researcher / Lecturer.
* Tạo link thanh toán PayOS trực tuyến, kiểm tra tính toàn vẹn chữ ký số Webhook (`ChecksumKey`) và kích hoạt thuê bao tự động qua Database Transaction.

### 7. 💬 Diễn đàn & Kiểm duyệt Nội dung (Forum & Moderation)
* Đăng bài thảo luận học thuật, bình luận lồng nhau (Nested comments), upvote/downvote.
* Báo cáo vi phạm (`ContentReports`) cho bài viết, bình luận, bài báo hoặc người dùng.
* Hệ thống lưu vết nhật ký (`AuditLogs`) phục vụ an toàn và kiểm toán.

---

## ⚙️ Hướng Dẫn Cài Đặt & Chạy Local (Getting Started)

### 1. Yêu cầu môi trường (Prerequisites)
* **.NET 8.0 SDK** ([Tải về](https://dotnet.microsoft.com/download/dotnet/8.0))
* **SQL Server 2019+** hoặc **SQL Server Express / LocalDB**
* **Visual Studio 2022** / **VS Code** / **Rider**

### 2. Cấu hình `appsettings.json`
Mở file `ARSPlatform.API/appsettings.json` và cấu hình các thông số cần thiết:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ARSFlatformDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "ARSPlatform_DevOnly_LocalJwtSecretKey_ChangeInProduction_AtLeast32Bytes",
    "Issuer": "ARSPlatform",
    "Audience": "ARSPlatformUsers",
    "ExpireMinutes": 1440
  },
  "GeminiSettings": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-3.7-flash"
  },
  "PayOSSettings": {
    "ClientId": "YOUR_PAYOS_CLIENT_ID",
    "ApiKey": "YOUR_PAYOS_API_KEY",
    "ChecksumKey": "YOUR_PAYOS_CHECKSUM_KEY"
  }
}
```

### 3. Cập nhật Cơ sở dữ liệu (Database Migration)
Mở terminal tại thư mục gốc của dự án và chạy:

```bash
dotnet ef database update --project ARSPlatform.MODEL --startup-project ARSPlatform.API
```

### 4. Build và Chạy API
```bash
# Khôi phục packages
dotnet restore

# Build dự án
dotnet build

# Chạy Backend API
dotnet run --project ARSPlatform.API
```

API sẽ khởi chạy tại:
* **HTTP:** `http://localhost:5000`
* **HTTPS:** `https://localhost:5001`
* **Swagger UI:** `http://localhost:5000/swagger`

---

## 🐳 Chạy bằng Docker

```bash
# Build Docker image
docker build -t ars-backend .

# Chạy Docker container
docker run -d -p 5000:8080 --name ars-api ars-backend
```

---

## 🔑 Tài Khoản Thử Nghiệm Mặc Định (Seed Accounts)

| Vai trò | Email | Mật khẩu mặc định |
| :--- | :--- | :--- |
| **Admin** | `admin@arsplatform.com` | `Password123` |
| **Researcher** | `researcher@arsplatform.com` | `Password123` |
| **Lecturer** | `lecturer@arsplatform.com` | `Password123` |
| **Reviewer** | `reviewer@arsplatform.com` | `Password123` |
| **Graduate Student** | `student@arsplatform.com` | `Password123` |

---

## 🧪 Kiểm Thử (Testing)

```bash
# Chạy toàn bộ Unit Tests
dotnet test
```

---

© 2026 **ARS Platform Team**. All rights reserved.
