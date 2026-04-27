# CamAI - Chi tiết Hệ thống AI Nhận diện Khuôn mặt

Tài liệu này mô tả chi tiết các thành phần AI, quy trình xử lý và các tham số kỹ thuật quan trọng của hệ thống nhận diện khuôn mặt trong dự án CamAI.

## 1. Thành phần Model AI

Hệ thống sử dụng tổ hợp hai model tiên tiến nhất từ OpenCV Zoo để đảm bảo tốc độ và độ chính xác:

### 1.1 Face Detection (Phát hiện khuôn mặt): **YuNet**
- **Model:** `yunet.onnx` (Phiên bản chính thức 2023)
- **Đặc điểm:** Cấu trúc CNN siêu nhẹ, tối ưu cho CPU. 
- **Đầu ra:** Tọa độ khung mặt (Bounding Box) và **5 điểm mốc (Landmarks)**: Mắt trái, mắt phải, mũi, khóe miệng trái/phải.
- **Tối ưu trong CamAI:** Ảnh đầu vào được scale về tối đa 720px để cân bằng giữa độ chính xác và tốc độ.
- **Lọc chất lượng mặt:** Hệ thống áp dụng ngưỡng Confidence tối thiểu (>0.7) cho các cảnh báo người lạ để loại bỏ các trường hợp mặt bị che khuất hoặc quay đi quá sâu causing sai lệch nhận diện.

### 1.2 Face Recognition (Định danh): **SFace**
- **Model:** `sface.onnx` (Nghiên cứu bởi Đại học Sáng kiến Bắc Kinh)
- **Đặc điểm:** Được thiết kế để phối hợp hoàn hảo với YuNet.
- **Đầu ra:** Vector đặc trưng (Embedding) 128 chiều.
- **Dải dữ liệu:** Input là ảnh BGR, giá trị pixel [0, 255]. Không cần trừ mean hay chia std phức tạp như các dòng ArcFace cũ.

---

## 2. Quy trình Xử lý (Pipeline)

Quy trình để nhận diện một khuôn mặt từ luồng RTSP/Ảnh được thực hiện qua các bước nghiêm ngặt:

1.  **Detection:** YuNet xác định vị trí mặt và bóc tách 5 điểm mốc.
2.  **Face Alignment (Căn chỉnh):** 
    - Sử dụng thuật toán **Umeyama Similarity Transform**.
    - Đây là tiêu chuẩn vàng của InsightFace/ArcFace, giúp tính toán ma trận xoay, tỉ lệ và tịnh tiến tối ưu nhất từ 5 điểm mốc landmarks.
    - Kết quả: Khuôn mặt luôn được đưa về vị trí "chính diện" chuẩn trong khung hình 112x112, giúp vector SFace đạt độ ổn định cao nhất ngay cả khi người dùng đứng nghiêng.
    - **Lưu ý:** Đã loại bỏ xử lý CLAHE vì nó gây nhiễu giá trị pixel trong điều kiện ánh sáng thay đổi.
3.  **Preprocessing:** Ảnh sau align được đưa về kích thước 112x112, định dạng Planar BGR.
4.  **Inference:** SFace trích xuất vector 128 chiều đại diện cho danh tính khuôn mặt.
5.  **L2 Normalization:** Chuẩn hóa vector về độ dài 1.0 để thực hiện so khớp Cosine.

---

## 3. So khớp và Ngưỡng Nhận diện (Thresholding)

Hệ thống sử dụng **Cosine Similarity** để đo độ tương đồng giữa vector khuôn mặt đang xét và các khuôn mặt trong cơ sở dữ liệu.

### 3.1 Tham số Cấu hình
- **Độ dài Vector:** 128 chiều.
- **Ngưỡng mặc định (Default Threshold):** `0.35`.

### 3.3 Hệ thống Chống Spam Log (Anti-Spam Logic)
Để đảm bảo nhật ký truy cập không bị "rác", hệ thống áp dụng các quy tắc:
- **Confirmation Counter:** Yêu cầu nhận diện chính xác 7 frame AI liên tiếp (~1.2 giây) trước khi ghi log.
- **Spatial Hashing:** Nhóm các lượt nhận diện người lạ theo vùng không gian (grid 100x100px) để tránh việc cùng 1 người lạ đứng im gây ra hàng chục log.
- **Frontality Check:** Chỉ ghi log "Người lạ" khi mặt đang quay chính diện (tỉ lệ lệch mũi so với 2 mắt < 25%). Nếu người quen quay mặt, AI có thể nhận nhầm là người lạ, nhưng bộ lọc này sẽ chặn không cho ghi log sai.
- **Size Filter:** Bỏ qua các khuôn mặt nhỏ hơn 80px (quá xa camera).
- **Cooldown:** Người quen (5 phút/log), Người lạ (2 phút/log).

---

## 4. Lưu ý Kỹ thuật

- **Lưu trữ Embedding:** Vector khuôn mặt (`float[]`) được chuyển thành `byte[]` và lưu vào SQL Server dưới dạng `VARBINARY(MAX)` thông qua Stored Procedure `sp_FaceProfile_Register`.
- **Đồng bộ RAM:** Khi AI Engine khởi động, toàn bộ Embedding được tải từ `CamAI.API` vào `Dictionary<Guid, RegisteredFace>` trên RAM. Dữ liệu được đồng bộ lại định kỳ mỗi 1 phút.
- **Thread Safety:** Dùng `lock` trong `ApiFaceMatchService` để đảm bảo an toàn khi nhiều camera cùng nhận diện và đăng ký một lúc.
- **Dependencies:** Sử dụng `OpenCvSharp4` cho xử lý ảnh và `Microsoft.ML.OnnxRuntime` để chạy inference AI.
