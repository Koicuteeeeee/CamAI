# DATA SCHEMA: SQL SERVER & MINIO STORAGE

## 1. Database Tables

### 1.1. Users
Lưu trữ thông tin định danh cơ bản của người dùng.
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `Username` (NVARCHAR(50), UNIQUE)
*   `FullName` (NVARCHAR(100))
*   `CreatedAt` (DATETIME, Default: GETDATE())
*   `IsActive` (BIT, Default: 1)

### 1.2. UserFaces
Lưu trữ vector đặc trưng (Embedding) và đường dẫn ảnh mẫu đa góc độ.
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `UserId` (UNIQUEIDENTIFIER, FK -> Users.Id)
*   `EmbeddingFront` (VARBINARY(MAX)) - Vector mặt chính diện.
*   `EmbeddingLeft` (VARBINARY(MAX)) - Vector mặt nghiêng trái.
*   `EmbeddingRight` (VARBINARY(MAX)) - Vector mặt nghiêng phải.
*   `MinioFront` (NVARCHAR(500)) - Đường dẫn ảnh chính diện trên MinIO.
*   `MinioLeft` (NVARCHAR(500)) - Đường dẫn ảnh trái trên MinIO.
*   `MinioRight` (NVARCHAR(500)) - Đường dẫn ảnh phải trên MinIO.
*   `CreatedAt` (DATETIME)

### 1.3. AccessLogs
Lưu nhật ký nhận diện và an ninh thời gian thực.
*   `Id` (UNIQUEIDENTIFIER, PK, Default: NEWID())
*   `LogTime` (DATETIME)
*   `UserId` (UNIQUEIDENTIFIER, NULL) - ID người dùng nếu nhận diện được (KNOWN).
*   `FullName` (NVARCHAR(100)) - Tên hiển thị lúc nhận diện.
*   `RecognitionStatus` (NVARCHAR(20)) - `IDENTIFIED` hoặc `UNKNOWN`.
*   `Similarity` (FLOAT) - Độ tương đồng cao nhất khi so khớp AI.
*   `MinioLogImage` (NVARCHAR(500)) - Link ảnh bằng chứng trong MinIO.

---

## 2. MinIO Storage Hierarchy (Bucket: `faces`)

Hệ thống sử dụng một Bucket duy nhất là `faces` để tối ưu hóa quản lý, phân cấp theo thời gian:

| Phân loại | Cấu trúc thư mục | Ghi chú |
| :--- | :--- | :--- |
| **Ảnh Đăng ký** | `register/yyyy/MM/dd/{personId}_{angle}.jpg` | Lưu 3 ảnh chuẩn (front, left, right). |
| **Ảnh Nhật ký** | `logs/{identified|alerts}/yyyy/MM/dd/{guid}.jpg` | Phân loại giữa người quen và người lạ. |

---

## 3. Stored Procedures (Tiêu chuẩn DAL)

| Tên Procedure | Trách nhiệm |
| :--- | :--- |
| `sp_User_Register` | Đăng ký User mới kèm 3 góc mặt và 3 đường dẫn ảnh MinIO. |
| `sp_UserFace_GetAll` | Lấy toàn bộ Embedding (3 góc) để đồng bộ lên AI RAM. |
| `sp_AccessLog_Insert` | Lưu vết nhận diện kèm link ảnh bằng chứng. |
| `sp_User_GetAllActive` | Lấy danh sách toàn bộ người dùng đang hoạt động. |
| `sp_User_GetById` | Lấy thông tin chi tiết một User. |

---

## 4. Quy tắc DAL
*   Mọi thao tác dữ liệu bắt buộc qua Stored Procedures.
*   Embeddings được lưu dưới dạng `VARBINARY(MAX)` (mảng byte từ `float[]`).
*   Đường dẫn MinIO được lưu dưới dạng tương đối hoặc tuyệt đối tùy cấu hình nhưng phải theo chuẩn phân cấp thời gian.
