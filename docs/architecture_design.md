# ARCHITECTURE DESIGN: CAMAI SYSTEM

## 1. Architectural Pattern: Distributed 3-Layer

Hệ thống được thiết kế theo kiến trúc Microservices phân tán, tách biệt giữa tầng xử lý AI thời gian thực và tầng quản lý dữ liệu trung tâm. Cả hai dự án đều tuân thủ mô hình **API - BLL - DAL**.

### 1.1. Data API Service (CamAI.API)
*   **Trách nhiệm:** Lưu trữ tập trung Metadata, Embeddings và Access Logs.
*   **Cơ chế:** Sử dụng Dapper + Stored Procedures để đảm bảo tính toàn vẹn dữ liệu.
*   **Database:** SQL Server phân tách bảng `Users` và `UserFaces` để hỗ trợ đa mẫu mã khuôn mặt trên mỗi người dùng.

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

### 2.2. Enrollment Workflow (3-Angle Capture)
Để tăng độ chính xác khi nhận diện từ các góc độ khác nhau:
1.  **Chế độ Đăng ký (Enrollment Mode)**: Yêu cầu người dùng nhìn thẳng, quay trái và quay phải.
2.  **Trích xuất**: Hệ thống lấy 3 Vector (Front, Left, Right) và 3 ảnh tương ứng.
3.  **Lưu trữ**: Đồng bộ cả 3 Vector về SQL Server và 3 ảnh lên MinIO.
4.  **Nhận diện (Inference)**: AI sẽ so khớp mặt hiện tại với cả 3 góc độ trong DB, lấy kết quả có độ tương đồng (`Similarity`) cao nhất.

---

## 3. Streaming & Visual Debugging
*   **Luồng Stream:** Cung cấp MJPEG Stream tại `/api/FaceStream/live` phục vụ Debug.
*   **Logic vẽ khung:**
    *   **Màu Xanh**: Người đã đăng ký (Kèm tên và độ Similarity).
    *   **Màu Đỏ**: Người chưa đăng ký hoặc đang trong quá trình đăng ký (`ALREADY REGISTERED` check).
*   **Tốc độ:** Khóa cứng luồng trả về tại ~33ms (30 FPS) để khớp với tốc độ Video thực tế.

---

## 4. Technical Stack
*   **Core:** .NET 9
*   **Computer Vision:** OpenCvSharp4, ONNX Runtime (CPU/GPU acceleration).
*   **Database:** SQL Server (Dapper).
*   **Storage:** MinIO SDK (S3 Interface).
*   **Connectivity:** Tailscale VPN (Connect to remote IP Cameras).
