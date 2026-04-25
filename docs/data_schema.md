# DATA SCHEMA: SQL SERVER & STORED PROCEDURES

## 1. Database Tables

### 1.1. Users
Lưu trữ thông tin người dùng được quyền truy cập hệ thống.
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `Username` (NVARCHAR(50))
*   `FullName` (NVARCHAR(100))
*   `CreatedAt` (DATETIME)
*   `IsActive` (BIT)

### 1.2. UserFaces
Lưu trữ thông tin ảnh và vector đặc trưng (Embedding) của khuôn mặt.
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `UserId` (UNIQUEIDENTIFIER, FK -> Users.Id)
*   `MinioObjectName` (NVARCHAR(255)) - Tên file trong MinIO.
*   `FaceEmbedding` (VARBINARY(MAX)) - Dãy số đặc trưng lưu dưới dạng mảng byte.
*   `CreatedAt` (DATETIME)

### 1.3. AccessLogs
Lưu nhật ký tương tác với hệ thống.
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `LogTime` (DATETIME)
*   `UserId` (UNIQUEIDENTIFIER, NULL) - NULL nếu là người lạ.
*   `ActionTaken` (NVARCHAR(100)) - Cử chỉ đã nhận diện.
*   `MinioLogImage` (NVARCHAR(255)) - Link ảnh chụp lại lúc đó trong MinIO.
*   `DeviceImpacted` (NVARCHAR(100)) - Thiết bị đã điều khiển.

---

## 2. Stored Procedures (Tiêu chuẩn DAL)

| Tên Procedure | Trách nhiệm |
| :--- | :--- |
| `sp_User_GetByFaceEmbedding` | So sánh embedding đầu vào với DB để tìm User tương ứng. |
| `sp_User_Register` | Tạo User mới và lưu ảnh mặt mẫu. |
| `sp_AccessLog_Insert` | Lưu một bản ghi lịch sử tương tác mới. |
| `sp_User_GetAllActive` | Lấy danh sách toàn bộ người dùng đang hoạt động. |
| `sp_AccessLog_GetHistory` | Truy xuất lịch sử tương tác để hiển thị lên Website. |

---

## 3. Quy tắc DAL
*   Tầng DAL không được viết câu lệnh SQL (SELECT/INSERT/UPDATE) trực tiếp. 
*   Mọi thao tác phải thông qua `SqlCommand.CommandType = CommandType.StoredProcedure`.
