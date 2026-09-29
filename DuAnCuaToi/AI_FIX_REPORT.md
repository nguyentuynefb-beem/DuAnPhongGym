# Báo cáo sửa lỗi SmartGym AI

## Nguyên nhân chính đã tìm thấy

1. `GeminiService` khai báo `_model` nhưng không hề gán giá trị. Hàm gọi API lại dùng `_model` thay vì `_primaryModel` / `_fallbackModel`.
   - Lần gọi đầu tạo endpoint kiểu `.../models/:generateContent`.
   - Sau đó code chỉ fallback cứng sang `gemini-3.5-flash`, bỏ qua cấu hình trong `appsettings.json`.

2. Lỗi API bị trả về như một chuỗi bình thường.
   - Khi 429/500/503 lặp lại, service trả chuỗi "AI hiện đang tạm thời quá tải...".
   - Controller vẫn trả `{ success: true }`, nên UI hiển thị lỗi như thể đó là kết quả AI hợp lệ.

3. API key được ghi trực tiếp trong `appsettings.json`.
   - Key cũ phải được revoke/rotate vì đã nằm trong source/project được chia sẻ.

4. Parser chỉ đọc `candidates[0].content.parts[0].text`.
   - Response Gemini có thể có nhiều `parts`; cách cũ có thể bỏ sót text.

5. `Views/User/Index.cshtml` chứa CSS `#aiOutput { ... }` ngay bên trong thẻ `<script>`.
   - Đây là lỗi cú pháp JavaScript và có thể làm hỏng toàn bộ script AI trên trang dashboard hội viên.

6. FR8 chỉ gửi cân nặng/chiều cao dù CSDL còn có vòng ngực/vòng eo/vòng hông/tình trạng sức khỏe.

7. Context gửi AI có dữ liệu định danh không cần thiết và frontend một số chỗ gọi `response.json()` trực tiếp, làm thông báo lỗi khó hiểu nếu server trả HTML/text.

## Các thay đổi đã thực hiện

- Dùng đúng primary/fallback model từ cấu hình.
- Primary mặc định: `gemini-3.8-flash`.
- Fallback: `gemini-3.5-flash-lite`.
- Retry có exponential backoff + jitter cho 408/429/5xx/network.
- Phân biệt lỗi 400/401/403/404/408/429/5xx và trả `success=false` đúng nghĩa.
- Thêm log server cho lỗi Gemini, không trả raw exception cho client.
- Đọc tất cả text parts trong response và bỏ thought parts nếu có.
- Giảm temperature cho báo cáo/tóm tắt để bớt bịa và ổn định hơn.
- Tăng `maxOutputTokens` cho các tác vụ dùng thinking để giảm nguy cơ output bị cắt.
- Bổ sung dữ liệu vòng đo/tình trạng sức khỏe vào AI health context.
- Giảm PII ở FR8; chat chỉ gửi tên/mã hội viên khi người dùng thực sự hỏi thông tin nhận dạng.
- Thêm chống prompt-injection cho context trong prompt FR8/FR9.
- Dashboard chat có lịch sử hội thoại ngắn giống trang Trợ lý AI.
- Sửa CSS bị đặt sai trong `<script>` ở `Views/User/Index.cshtml`.
- Frontend đọc response an toàn hơn khi server trả lỗi không phải JSON.
- API key chuyển sang biến môi trường `GEMINI_API_KEY`.

## Việc cần làm trước khi chạy

Xem `AI_SETUP.md` và tạo Gemini API key mới. Không dùng lại key cũ đã từng nằm trong `appsettings.json`.
