# HƯỚNG DẪN DÀNH CHO LẬP TRÌNH VIÊN – ENV-ANALYSIS

## Mục đích tài liệu này
Tài liệu này nhằm giúp các thành viên mới hiểu cách setup môi trường, cấu trúc dự án, quy trình dùng Git và đẩy code đúng cách.

## YÊU CẦU MÁY PHÁT TRIỂN
- Visual Studio 2022 trở lên (với workload ASP.NET và phát triển Web)
- .NET SDK (phiên bản dự án đang sử dụng – kiểm tra file .csproj hoặc global.json)
- SQL Server hoặc hệ quản trị CSDL tương ứng
- Git
- (Nếu có frontend độc lập) Node.js + npm

## CẤU TRÚC DỰ ÁN
Giả sử cấu trúc thư mục của dự án như sau:
env-analysis/
├─ EnvAnalysis.sln ← File Solution
├─ EnvAnalysis/ ← Project chính (Controllers, Views, Models, v.v.)
├─ EnvAnalysis.Web/ ← Nếu có phần frontend riêng
├─ EnvAnalysis.Tests/ ← Nếu có thư viện test
├─ README.md
└─ Setup_tutorial.txt
text### Các thư mục quan trọng:
- **EnvAnalysis/**: chứa mã nguồn chính ASP.NET MVC
- **Views/, Controllers/, Models/**: phần MVC quen thuộc
- **wwwroot/, Content/, Scripts/**: chứa file tĩnh (CSS, JS) nếu có
- **appsettings.json**: chứa thông tin cấu hình kết nối DB, logging, v.v.

## CÀI ĐẶT BAN ĐẦU (CHO DEV MỚI)

### Bước 1: Clone dự án từ GitHub
git clone https://github.com/KhanhNguyenVimaru/env-analysis.git
cd env-analysis
text### Bước 2: Mở solution trong Visual Studio
- Mở file `.sln`
- Visual Studio sẽ tự động restore các gói NuGet

### Bước 3: Tạo file cấu hình môi trường (nếu cần)
- Kiểm tra nếu có file mẫu: `appsettings.Development.json` hoặc `appsettings.json.sample`
- Nếu chưa có, tạo file `appsettings.json` và điền thông tin kết nối cơ bản như sau:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=EnvAnalysisDb;Trusted_Connection=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
Bước 4: Thiết lập cơ sở dữ liệu (nếu có migration hoặc seed dữ liệu)

Mở Package Manager Console trong Visual Studio
Chọn project chứa DbContext làm Startup Project
Chạy lệnh:

textUpdate-Database

Nếu có dữ liệu mẫu, nó sẽ tự động được thêm
Nếu không có migration, có thể import bằng script SQL nếu có kèm tài liệu

Bước 5: Chạy ứng dụng

Chọn project MVC làm Startup Project
Nhấn F5 hoặc nút "Run" để khởi động
Kiểm tra trang localhost hoạt động bình thường

Bước 6: (Nếu có frontend riêng biệt)

Di chuyển vào thư mục frontend
Cài đặt gói npm:

textnpm install

Chạy server:

textnpm run dev

Kết nối frontend → backend nếu cần qua CORS hoặc proxy

QUY TRÌNH DÙNG GIT & PUSH CODE
Lấy code mới nhất trước khi làm việc:
textgit pull origin main
Tạo nhánh mới cho từng tính năng hoặc lỗi:
textgit checkout -b feature/ten-tinh-nang
Thực hiện code và commit:
textgit add .
git commit -m "feat: thêm controller Emission và view"

Nếu là commit sửa lỗi:

textgit commit -m "fix: sửa bug tính toán emission"
Đẩy nhánh lên GitHub:
textgit push origin feature/ten-tinh-nang
Tạo Pull Request (PR):

Truy cập GitHub
Chọn nhánh vừa push
Nhấn “Compare & pull request”
Ghi rõ mô tả và tag người review
Sau khi được duyệt, merge vào main

Cập nhật nhánh chính khi có thay đổi mới:
textgit checkout main
git pull origin main
git merge feature/ten-tinh-nang
QUY TẮC COMMIT

Chia nhỏ commit, mỗi commit cho một tính năng hoặc thay đổi cụ thể
Ghi rõ ràng, có cấu trúc như sau: <loại>: <mô tả ngắn>

Ví dụ:

feat: thêm bảng EmissionData
fix: sửa lỗi validate đầu vào
refactor: tái cấu trúc service logic
docs: cập nhật hướng dẫn setup

LỖI THƯỜNG GẶP & CÁCH XỬ LÝ
Lỗi “Permission denied” khi git add .

Nguyên nhân: file .vs/, bin/, obj/ đang bị Visual Studio khóa
Cách khắc phục: đóng Visual Studio và chạy lại git add .
Đảm bảo có file .gitignore với nội dung sau:

text.vs/
bin/
obj/
*.user
*.suo
*.cache
*.log
Lỗi migration hoặc database không đồng bộ

Kiểm tra lại chuỗi kết nối (Connection String)
Xóa migration cũ, tạo lại nếu cần
Đảm bảo bạn có quyền tạo / chỉnh sửa database