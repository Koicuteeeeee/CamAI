# ARCHITECTURE DESIGN: CAMAI SYSTEM

## 1. Architectural Pattern: 3-Layer + Microservices

Hệ thống được thiết kế theo kiến trúc Microservices, tách biệt giữa tầng xử lý AI (Inference) và tầng quản lý dữ liệu (Data Management).

### 1.1. Data API Service (CamAI.API)
*   **Công nghệ:** .NET 9 + Dapper.
*   **Cổng mặc định:** `5282`.
*   **Cấu trúc:** 3-Layer (API - BLL - DAL).
*   **Trách nhiệm:** 
    *   Quản lý tập trung toàn bộ dữ liệu người dùng, embeddings, và nhật ký (Access Logs) trong SQL Server.
    *   Cung cấp API REST cho AI Engine đồng bộ hóa dữ liệu khuôn mặt.
    *   Thực thi logic nghiệp vụ qua Stored Procedures để đảm bảo hiệu năng và bảo mật.

### 1.2. AI Engine Service (CamAI.Service.AI)
*   **Công nghệ:** .NET 9 + OpenCvSharp4 + ONNX Runtime.
*   **Cổng mặc định:** `5120`.
*   **Cấu trúc:** 3-Layer (API - BLL - Infrastructure).
*   **Trách nhiệm:** 
    *   Thực hiện nhận diện khuôn mặt (Face Recognition) và cử chỉ thời gian thực.
    *   **Stateless Local:** Không lưu dữ liệu người dùng tại chỗ, tự động đồng bộ từ `CamAI.API` lên RAM khi khởi động.
    *   Trực tiếp xử lý đẩy ảnh (Snapshots/Face) lên **MinIO** và sau đó gửi Metadata về cho `CamAI.API`.
    *   **Multi-Camera Support:** Cho phép cấu hình và xử lý AI song song cho nhiều camera cùng lúc qua file cấu hình.

### 1.3. Visual Debug Stream (MJPEG)
*   **Truy cập:** `http://localhost:5120/api/FaceStream/live`
*   **Trách nhiệm:** Cung cấp luồng video trực tiếp có vẽ khung nhận diện (Bounding Boxes) và tên người dùng để giám sát và gỡ lỗi.

---

## 2. Infrastructure
*   **Shared (CamAI.Common):** Chứa Interfaces, Models dùng chung.
*   **Object Storage (MinIO):** Chạy trên Docker (Port 9000/9001). Lưu trữ ảnh vật lý.
*   **Database (SQL Server):** Lưu trữ Metadata & Embeddings. Sử dụng kiểu dữ liệu `UNIQUEIDENTIFIER` (GUID) cho toàn bộ ID.

---

## 3. Tech Stack Summary
*   **Runtime:** .NET 9.
*   **Computer Vision:** OpenCvSharp4 + YuNet (Detector) + SFace (Embedder).
*   **Database:** SQL Server + Dapper (High performance).
*   **Object Storage:** MinIO SDK (S3 Compatible).
*   **Containerization:** Docker Compose for Infrastructure.
*   **Networking:** Tailscale Mesh VPN (để kết nối camera từ xa qua mạng khác).

---

## 4. Configuration: Multi-Camera Management
Cấu hình danh sách Camera được quản lý tại file `appsettings.json` của AI Engine:

```json
"CameraSettings": {
  "Cameras": [
    {
      "Id": "Cam01",
      "Name": "Cổng Chính",
      "Url": "rtsp://100.120.x.x:8554/mystream"
    },
    {
      "Id": "Cam02",
      "Name": "Sân Sau",
      "Url": "rtsp://100.120.x.x:8554/garden"
    }
  ]
}
```
*Hệ thống sẽ tự động khởi tạo các Worker xử lý AI độc lập cho mỗi Camera khi khởi động.*

---

## 4. Interaction Flow (Registration)
1.  **Client** gửi ảnh qua API `Face/register` của **AI Engine** (`:5120`).
2.  **AI Engine** trích xuất vector (embedding) và đẩy ảnh gốc lên **MinIO** (`:9000`).
3.  **AI Engine** gọi API `Users/register` của **CamAI.API** (`:5282`), gửi kèm Vector và `MinioObjectName`.
4.  **CamAI.API** thực thi Stored Procedure lưu vào **SQL Server**.
5.  **AI Engine** tự động làm mới bộ nhớ RAM để nhận diện được người mới ngay lập tức.

## 5. Dependency Injection (DI)
Hệ thống sử dụng DI container mặc định của .NET Core để đăng ký các service:
*   `builder.Services.AddScoped<IUserRepository, UserRepository>();`
*   `builder.Services.AddScoped<IFaceAuthService, FaceAuthService>();`
*   `builder.Services.AddSingleton<IStorageService, MinioStorageService>();`
