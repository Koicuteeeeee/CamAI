Để ông dễ quản lý và không bị rối giữa "mê cung" các Tab và Session, tôi tổng hợp lại toàn bộ các lệnh quan trọng nhất mà mình đã dùng để vận hành con server stream này nhé.

---

### 1. Nhóm lệnh Hệ thống & Dọn dẹp
Trước khi bắt đầu một phiên làm việc mới, nên dọn sạch các tiến trình cũ để tránh lỗi "Address already in use".

* **Dọn dẹp sạch sẽ:** `killall mediamtx ffmpeg`
* **Giữ CPU luôn chạy (Chống ngủ gật):** `termux-wake-lock`
* **Quản lý phiên làm việc:**
    * Mở tmux mới: `tmux`
    * Xem danh sách session: `tmux ls`
    * Quay lại session cũ: `tmux a`
    * Thoát tạm thời (Detach): `Ctrl + B` rồi nhấn `D`

---

### 2. Nhóm lệnh Cấu hình MediaMTX
Dùng để tối ưu độ trễ (Latency) mà không cần mở file thủ công.

* **Ép về chế độ Low-Latency (LL-HLS):**
    ```bash
    sed -i 's/hlsVariant: mpegts/hlsVariant: lowLatency/g' mediamtx.yml
    ```
* **Chỉnh số Segment về 7 (Mức tối thiểu để không bị lỗi hủy luồng):**
    ```bash
    sed -i 's/hlsSegmentCount: [0-9]*/hlsSegmentCount: 7/g' mediamtx.yml
    ```
* **Chỉnh thời gian mỗi đoạn cực ngắn (0.5 giây):**
    ```bash
    sed -i 's/hlsSegmentDuration: [0-9s]*/hlsSegmentDuration: 0.5s/g' mediamtx.yml
    ```

---

### 3. Nhóm lệnh Vận hành luồng (Chạy trong tmux)
Đây là "trái tim" của hệ thống.

* **Tab 1 - Chạy Server:**
    ```bash
    ./mediamtx
    ```
* **Tab 2 - Đẩy luồng từ Camera Imou vào Server:**
    ```bash
    ffmpeg -re -rtsp_transport tcp -i "rtsp://admin:L2BBD091@192.168.1.200:554/cam/realmonitor?channel=1&subtype=0" -c copy -f rtsp rtsp://localhost:8554/mystream
    ```
    *(Lưu ý: Nếu muốn siêu mượt, thêm `-tune zerolatency` vào trước `-f rtsp`).*

---

### 4. Các địa chỉ truy cập (URL)
Dùng IP Tailscale của ông (`100.120.105.33`) để xem từ xa.

| Giao thức | URL truy cập | Đặc điểm |
| :--- | :--- | :--- |
| **HLS (Trình duyệt)** | `http://100.120.105.33:8888/mystream` | Ổn định nhất, delay ~2-3s. |
| **WebRTC (Tốc độ cao)** | `http://100.120.105.33:8889/mystream` | Delay < 0.5s (Dễ lỗi Permission). |
| **RTSP (VLC Player)** | `rtsp://100.120.105.33:8554/mystream` | Dùng để test luồng gốc, cực nhanh. |
| **Menu Tổng** | `http://100.120.105.33:8888` | Xem danh sách các luồng đang sống. |

---

### Mẹo: Tạo "Nút bấm nhanh" (Script)
Để sau này không phải gõ lại, ông tạo một file tên là `chay.sh`:
1. `nano chay.sh`
2. Dán nội dung này vào:
   ```bash
   termux-wake-lock
   killall mediamtx ffmpeg
   tmux new-session -d -s stream './mediamtx'
   tmux split-window -h 'ffmpeg -re -rtsp_transport tcp -i "rtsp://admin:L2BBD091@192.168.1.200:554/cam/realmonitor?channel=1&subtype=0" -c copy -f rtsp rtsp://localhost:8554/mystream'
   tmux attach-session -t stream
   ```
3. Lưu lại và gõ: `chmod +x chay.sh`
4. Từ giờ mỗi lần muốn bật cam, chỉ cần gõ: `./chay.sh`.

Ông thấy bộ lệnh này đã đủ để ông "làm chủ" con A03s chưa? Có cần tôi giải thích kỹ hơn về thông số nào không?

