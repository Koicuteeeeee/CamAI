# FUNCTIONAL SPECIFICATION: CAMAI GESTURE CONTROL SYSTEM

## 1. Context & Objective
Xây dựng hệ thống điều khiển thiết bị thông minh bằng cử chỉ tay, tích hợp lớp bảo mật nhận diện khuôn mặt. Chỉ những người dùng đã đăng ký khuôn mặt trong cơ sở dữ liệu mới được quyền sử dụng cử chỉ để điều khiển hệ thống.

*   **Mục tiêu:** Điều khiển Đèn, Quạt, Tivi... thông qua Camera.
*   **Bảo mật:** Face Recognition Authorization.

## 2. Core Features (Chức năng cốt lõi)

### 2.1. Face Recognition Layer
*   **Chế độ chờ:** Camera luôn quét để phát hiện khuôn mặt.
*   **Xác thực:** So khớp mặt người đang đứng trước cam với dữ liệu "Người quen". 
*   **Đăng ký:** Yêu cầu đăng ký 3 góc độ (Trực diện, Trái, Phải) để đảm bảo xác thực chính xác khi người dùng đứng nghiêng điều khiển thiết bị.
*   **Trạng thái:** Nếu là người quen, hệ thống chuyển sang trạng thái "Lắng nghe cử chỉ" (Listening) trong một khoảng thời gian nhất định (Timeout).

### 2.2. Gesture Control Layer
*   **Detect:** Nhận diện 21 điểm mốc bàn tay.
*   **Phân loại:**
    *   `PALM` (Xòe tay): Bật/Tắt chế độ chờ.
    *   `FIST` (Nắm tay): Tắt toàn bộ thiết bị.
    *   `V-SIGN` (Hai ngón): Bật/Tắt Đèn.
    *   `THUMB UP` (Like): Tăng tốc độ quạt.

### 2.3. Notification & UI
*   Hiển thị luồng video thời gian thực trên Web App.
*   Thông báo tên người đang điều khiển (vd: "Chào anh Nam, mời anh ra lệnh").
*   Hiển thị lịch sử điều khiển.

## 3. Reference Documents (Tài liệu kỹ thuật)
*   **[Architecture Design](./architecture_design.md)**: Chi tiết cấu trúc 3 lớp, công nghệ và luồng dữ liệu.
*   **[Data Schema](./data_schema.md)**: Chi tiết về SQL Server Tables và Stored Procedures.
*   **[AI Model Details](./ai_model_details.md)**: Chi tiết về YuNet, SFace và quy trình Face Alignment.
*   **[User Guide](./user_guide.md)**: Hướng dẫn sử dụng và đăng ký (Sắp có).

---

## 4. Specific Constraints
*   **Latency:** Độ trễ hệ thống < 300ms.
*   **Reliability:** Nhận diện đúng người quen > 95% trong điều kiện ánh sáng bình thường.
