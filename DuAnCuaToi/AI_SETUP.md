# SmartGym AI - cấu hình sau khi sửa

## 1. Quan trọng: tạo lại Gemini API key
API key trước đây nằm trực tiếp trong `appsettings.json`, vì vậy hãy thu hồi (revoke) key cũ trong Google AI Studio và tạo key mới.
Không commit key mới vào Git/source code.

## 2. Cấu hình key bằng biến môi trường
`GeminiService` ưu tiên biến môi trường `GEMINI_API_KEY`.

PowerShell (chỉ phiên terminal hiện tại):

```powershell
$env:GEMINI_API_KEY="KEY_MOI_CUA_BAN"
dotnet run
```

Thiết lập cho tài khoản Windows (mở terminal mới sau khi chạy):

```powershell
[Environment]::SetEnvironmentVariable("GEMINI_API_KEY", "KEY_MOI_CUA_BAN", "User")
```

Hoặc có thể dùng biến cấu hình ASP.NET Core `Gemini__ApiKey` nếu muốn.

## 3. Model
Mặc định đã đổi thành:
- Primary: `gemini-3.8-flash`
- Fallback: `gemini-3.5-flash-lite`

Có thể đổi trong `appsettings.json` mà không sửa code.

## 4. Khi AI lỗi
Ứng dụng giờ phân biệt được:
- 401/403: key/quyền truy cập
- 404: model không khả dụng
- 429: quota/rate limit
- 5xx: dịch vụ Gemini tạm lỗi
- timeout/network: kết nối chậm hoặc mất kết nối

Chi tiết kỹ thuật được ghi vào log server; frontend chỉ nhận thông báo an toàn.
