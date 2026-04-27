# DATA SCHEMA: SQL SERVER & MINIO STORAGE

## 1. Database Tables

### 1.1. Users (Tài khoản CMS)
Lưu trữ thông tin tài khoản quản trị, liên kết với Keycloak IAM.
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `KeycloakId` (UNIQUEIDENTIFIER, UNIQUE, NULL) - Liên kết với Keycloak.
*   `Username` (NVARCHAR(50), UNIQUE)
*   `Email` (NVARCHAR(100), NULL)
*   `FullName` (NVARCHAR(100))
*   `Role` (NVARCHAR(20), Default: 'Staff')
*   `IsActive` (BIT, Default: 1)
*   `CreatedAt` / `UpdatedAt` (DATETIME)
*   `CreatedBy` / `UpdatedBy` (NVARCHAR(100))

### 1.2. FaceProfiles (Hồ sơ nhận diện)
Lưu trữ thông tin định danh khuôn mặt, **tách biệt hoàn toàn** với tài khoản CMS.
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `FullName` (NVARCHAR(100)) - Tên hiển thị khi nhận diện.
*   `ExternalCode` (NVARCHAR(50), NULL) - Mã nhân viên hoặc mã ngoại bộ.
*   `ProfileType` (NVARCHAR(20), Default: 'Resident') - Phân loại: Resident, Guest, Staff...
*   `CreatedAt` / `UpdatedAt` (DATETIME)
*   `CreatedBy` / `UpdatedBy` (NVARCHAR(100))

### 1.3. FaceEmbeddings (Vector khuôn mặt - V2)
Lưu trữ vector đặc trưng (Embedding) và đường dẫn ảnh mẫu cho **N góc độ** khác nhau, liên kết **FK → FaceProfiles.Id** (ON DELETE CASCADE).
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `ProfileId` (UNIQUEIDENTIFIER, FK → FaceProfiles.Id)
*   `AngleLabel` (NVARCHAR(30)) - Nhãn góc mặt (`front`, `angle_15`, `left`, `right`...).
*   `AngleDegree` (FLOAT, NULL) - Độ xoay ước lượng của khuôn mặt (Yaw).
*   `Embedding` (VARBINARY(MAX)) - Vector đặc trưng (128-dim SFace).
*   `MinioImageUrl` (NVARCHAR(500)) - Đường dẫn ảnh cụ thể của góc này trên MinIO.
*   `CaptureQuality` (FLOAT) - Điểm chất lượng ảnh (YuNet Confidence).
*   `CreatedAt` (DATETIME)
*   `CreatedBy` (NVARCHAR(100))

### 1.4. AccessLogs (Nhật ký nhận diện)
Lưu nhật ký nhận diện và an ninh thời gian thực, liên kết **FK → FaceProfiles.Id** (ON DELETE SET NULL).
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `LogTime` (DATETIME, Default: GETDATE())
*   `ProfileId` (UNIQUEIDENTIFIER, NULL) - ID hồ sơ nếu nhận diện được (KNOWN).
*   `FullName` (NVARCHAR(100)) - Tên hiển thị lúc nhận diện.
*   `RecognitionStatus` (NVARCHAR(20)) - `IDENTIFIED` hoặc `UNKNOWN`.
*   `Similarity` (FLOAT) - Độ tương đồng cao nhất khi so khớp AI.
*   `MinioLogImage` (NVARCHAR(500)) - Link ảnh bằng chứng trong MinIO.
*   `DeviceImpacted` (NVARCHAR(100)) - Thiết bị liên quan (nếu có).
*   `CreatedAt` / `UpdatedAt` (DATETIME)
*   `CreatedBy` / `UpdatedBy` (NVARCHAR(100))

---

## 2. Mối quan hệ giữa các bảng

```
Users (CMS Accounts - Keycloak)
  └── Quản lý quản trị, không liên quan trực tiếp logic AI.

FaceProfiles (Hồ sơ nhận diện)
  ├── 1:N → FaceEmbeddings (Đa góc độ)      [ON DELETE CASCADE]
  └── 1:N → AccessLogs (Nhật ký ra vào)     [ON DELETE SET NULL]
```

> **Thiết kế Hybrid:** Tài khoản CMS (`Users`) và Hồ sơ nhận diện (`FaceProfiles`) được tách biệt hoàn toàn. Một người có thể có hồ sơ khuôn mặt mà không cần tài khoản CMS, và ngược lại.

---

## 3. MinIO Storage Hierarchy (Bucket: `faces`)

| Phân loại | Cấu trúc thư mục | Ghi chú |
| :--- | :--- | :--- |
| **Ảnh Đăng ký (V2)** | `register/yyyy/MM/dd/{profileId}_{angleLabel}.jpg` | Lưu N ảnh đa góc độ. |
| **Ảnh Nhật ký** | `logs/{identified|alerts}/yyyy/MM/dd/{guid}.jpg` | Ảnh bằng chứng nhận diện. |

---

## 4. Stored Procedures (Tiêu chuẩn V2)

### 4.1. FaceProfile & Embedding Procedures
| Tên Procedure | Trách nhiệm |
| :--- | :--- |
| `sp_FaceProfile_RegisterV2` | Tạo Profile rỗng (V2) - Bước 1 của quy trình Continuous Enrollment. |
| `sp_FaceEmbedding_Add` | Thêm một góc mặt và vector mới cho profile hiện có - Bước 2 (Chạy lặp). |
| `sp_FaceEmbedding_GetAll` | Lấy toàn bộ Embedding của tất cả người dùng (V2) JOIN với tên. |
| `sp_FaceProfile_GetAll` | Lấy danh sách hồ sơ cơ bản. |
| `sp_FaceProfile_Delete` | Xóa hồ sơ (CASCADE xóa luôn toàn bộ Embedding liên quan). |

### 4.2. AccessLog Procedures
| Tên Procedure | Trách nhiệm |
| :--- | :--- |
| `sp_AccessLog_Insert` | Lưu vết nhận diện. |
| `sp_AccessLog_GetHistory` | Lấy lịch sử nhận diện phân trang. |

### 4.3. User Procedures (Legacy/CMS)
| Tên Procedure | Trách nhiệm |
| :--- | :--- |
| `sp_User_Register` | Đăng ký tài khoản CMS mới. |
| `sp_User_GetAllActive` | Lấy danh sách tài khoản CMS đang hoạt động. |
| `sp_User_GetById` | Lấy thông tin chi tiết một tài khoản CMS. |
| `sp_User_Delete` | Xóa tài khoản CMS. |

---

## 5. Quy tắc DAL
*   Mọi thao tác dữ liệu bắt buộc qua Stored Procedures.
*   Embeddings được lưu dưới dạng `VARBINARY(MAX)` (mảng byte từ `float[]`) trong SQL Server.
*   Vector khuôn mặt được Cache trên RAM (Dictionary) trong AI Engine và đồng bộ định kỳ từ API.
*   Đường dẫn MinIO tuân theo chuẩn phân cấp thời gian (Time-based Partitioning).
