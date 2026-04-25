# CamAI - Chi tiết Hệ thống AI Nhận diện Khuôn mặt

Tài liệu này mô tả chi tiết các thành phần AI, quy trình xử lý và các tham số kỹ thuật quan trọng của hệ thống nhận diện khuôn mặt trong dự án CamAI.

## 1. Thành phần Model AI

Hệ thống sử dụng tổ hợp hai model tiên tiến nhất từ OpenCV Zoo để đảm bảo tốc độ và độ chính xác:

### 1.1 Face Detection (Phát hiện khuôn mặt): **YuNet**
- **Model:** `yunet.onnx` (Phiên bản chính thức 2023)
- **Đặc điểm:** Cấu trúc CNN siêu nhẹ, tối ưu cho CPU. 
- **Đầu ra:** Tọa độ khung mặt (Bounding Box) và **5 điểm mốc (Landmarks)**: Mắt trái, mắt phải, mũi, khóe miệng trái/phải.
- **Tối ưu trong CamAI:** Ảnh đầu vào được scale về tối đa 640px trước khi dò quét để duy trì tốc độ ~30 FPS trên các máy tính thông thường.

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
    - Sử dụng thuật toán **Similarity Transform (Affine)**.
    - Dựa vào 5 điểm mốc để "xoay và nắn" khuôn mặt về tư thế thẳng đứng chuẩn mực (Mắt nằm ngang, mũi ở chính giữa).
    - Mục đích: Loại bỏ sự sai lệch do góc chụp nghiêng, đầu cúi/ngửa, giúp vector đặc trưng thu được ổn định nhất.
3.  **Preprocessing:** Ảnh sau align được đưa về kích thước 112x112, định dạng Planar BGR.
4.  **Inference:** SFace trích xuất vector 128 chiều đại diện cho danh tính khuôn mặt.
5.  **L2 Normalization:** Chuẩn hóa vector về độ dài 1.0 để thực hiện so khớp Cosine.

---

## 3. So khớp và Ngưỡng Nhận diện (Thresholding)

Hệ thống sử dụng **Cosine Similarity** để đo độ tương đồng giữa vector khuôn mặt đang xét và các khuôn mặt trong cơ sở dữ liệu.

### 3.1 Tham số Cấu hình
- **Độ dài Vector:** 128 chiều.
- **Ngưỡng mặc định (Default Threshold):** `0.35`.

### 3.2 Giải thích Similarity Score
- **0.45 - 0.70:** Người quen (Match). Điểm số này dao động tùy thuộc vào ánh sáng và độ sắc nét của Camera.
- **< 0.30:** Người lạ (Stranger).
- **0.00 - 0.20:** Vùng ảnh rác, nhiễu hoặc phông nền không phải mặt người.

> [!TIP]
> Do đặc thù Camera giám sát thường bị nén ảnh (Zalo/MJPEG), ngưỡng **0.35** là điểm cân bằng tốt nhất giữa việc tránh nhận nhầm và việc nhận diện linh hoạt khi người dùng đeo kính hoặc đội mũ.

---

## 4. Lưu ý Kỹ thuật

- **Bộ nhớ:** Hệ thống lưu trữ Embedding dưới dạng mảng `float[]` trong file JSON (`registered_faces.json`).
- **Thread Safety:** Dùng `lock` trong `InMemoryFaceMatchService` để đảm bảo an toàn khi nhiều camera cùng nhận diện và đăng ký một lúc.
- **Dependencies:** Sử dụng `OpenCvSharp4` cho xử lý ảnh và `Microsoft.ML.OnnxRuntime` để chạy inference AI.
