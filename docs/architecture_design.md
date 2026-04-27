# ARCHITECTURE DESIGN: CAMAI SYSTEM

## 1. Architectural Pattern: Distributed 3-Layer

Hệ thống được thiết kế theo kiến trúc Microservices phân tán, tách biệt giữa tầng xử lý AI thời gian thực và tầng quản lý dữ liệu trung tâm. Cả hai dự án đều tuân thủ mô hình **API - BLL - DAL**.

### 1.1. Data API Service (CamAI.API)
*   **Trách nhiệm:** Lưu trữ tập trung Metadata, Embeddings và Access Logs.
*   **Cơ chế:** Sử dụng Dapper + Stored Procedures để đảm bảo tính toàn vẹn dữ liệu.
*   **Database:** SQL Server theo mô hình **Hybrid Identity**:
    *   Bảng `Users` — Tài khoản CMS, liên kết Keycloak (quản trị hệ thống).
    *   Bảng `FaceProfiles` + `UserFaces` — Hồ sơ nhận diện khuôn mặt (AI Engine).
    *   Bảng `AccessLogs` — Nhật ký nhận diện, liên kết FK → `FaceProfiles`.

### 1.2. AI Engine Service (CamAI.Service.AI)
*   **Trách nhiệm:** Thu thập video RTSP, xử lý Object Detection (YuNet) và Face Recognition (SFace).
*   **Tối ưu hiệu năng:** 
    *   **Frame Skipping:** Chỉ chạy AI cứ mỗi 5 Frames để đảm bảo luồng Stream mượt mà 30 FPS.
    *   **Async Processing:** Xử lý ghi Log và Upload ảnh MinIO bất đồng bộ để tránh gây trễ (Lag) luồng stream.
    *   **Stateless:** Toàn bộ Vector khuôn mặt được Cache trên RAM (Dictionary) và đồng bộ từ API khi khởi động.

---

## 2. Infrastructure & Storage Strategy

### 2.1. Centralized Object Storage (MinIO)
Hệ thống hợp nhất toàn bộ tài nguyên hình ảnh vào Bucket `faces` với cấu trúc quản lý theo thời gian (**Time-based Partitioning**):
*   `register/yyyy/MM/dd/`: Lưu ảnh gốc khi đăng ký (Cung cấp bằng chứng pháp lý).
*   `logs/identified/yyyy/MM/dd/`: Lưu vết người quen.
*   `logs/alerts/yyyy/MM/dd/`: Lưu vết người lạ/cảnh báo.

### 2.2. Continuous Enrollment Workflow (V2)
Hệ thống sử dụng quy trình đăng ký hiện đại không yêu cầu thao tác thủ công:
1.  **Chế độ Đăng ký (Enrollment Mode)**: Người dùng quay mặt chậm trước camera. AI sẽ tự động tính toán góc xoay (Yaw) dựa trên landmarks.
2.  **Tự động trích xuất**: AI tự động lưu lại vector và ảnh mỗi khi người dùng đạt đến một góc xoay mới (ví dụ: -45°, -30°, 0°, +15°, +35°).
3.  **Lưu trữ Đa góc độ**: Hệ thống lưu N bản ghi vào bảng `FaceEmbeddings`, liên kết với cùng một Profile ID.
4.  **Nhận diện (Inference)**: AI so khớp mặt hiện tại với toàn bộ tập hợp các góc đã lưu của người đó. Kết quả nhận diện là giá trị **MAX Similarity** lớn nhất trong tập hợp.

---

## 3. Streaming & Dual-View Logic
Hệ thống cung cấp hai luồng stream song song để phục vụ các mục đích khác nhau:
*   **Luồng Sạch (`/api/FaceStream/live-clean`)**: Hiển thị trên Dashboard chính. Luồng này không chứa bất kỳ khung vẽ (boxes) hay chữ (labels) nào, mang lại trải nghiệm chuyên nghiệp cho người dùng.
*   **Luồng Debug (`/api/FaceStream/live`)**: Giữ nguyên toàn bộ các khung bao (rectangles) màu xanh/đỏ, tên và độ tin cậy để phục vụ công tác giám sát kỹ thuật và kiểm thử.
*   **Tốc độ:** Duy trì ổn định 25-30 FPS cho cả hai luồng.

---

---

## 4. Chiến lược Lưu trữ Bằng chứng (Evidence Cropping)

Để Dashboard hiển thị rõ nét và tiết kiệm băng thông, hệ thống áp dụng cơ chế:
- **Smart Cropping:** Thay vì lưu toàn bộ khung hình camera (gây mờ khi xem lại), AI tự động cắt chính xác vùng khuôn mặt từ ảnh gốc Full HD.
- **Context Padding:** Vùng cắt được mở rộng thêm 25% các hướng để lấy thêm context (tóc, vai, trang phục), giúp ảnh có tỉ lệ cân đối.
- **MinIO Hierarchy:** Toàn bộ ảnh crop được lưu vào Bucket `faces` với cấu trúc `logs/{type}/yyyy/MM/dd/{guid}.jpg`.

## 5. Danh mục Công nghệ (Technical Stack)

| Thành phần | Công nghệ |
| :--- | :--- |
| **AI Engine** | .NET 9, OpenCvSharp4, OnnxRuntime |
| **Backend API** | ASP.NET Core Web API, EF Core / Dapper |
| **Database** | SQL Server (Lưu Metadata & Embedding) |
| **Identity** | Keycloak (Quản lý User CMS) |
| **Frontend** | Angular 19+, Tailwind CSS (Dashboard Dashboard) |
| **Storage** | MinIO (Object Storage cho ảnh) |
| **Media** | MediaMTX (RTSP to MJPEG / HLS Gateway) |
| **Network** | Tailscale (Kết nối Camera từ xa qua WireGuard) |
