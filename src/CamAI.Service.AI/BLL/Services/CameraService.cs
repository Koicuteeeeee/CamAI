using System.IO;
using CamAI.Common.Models;
using OpenCvSharp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using CamAI.Service.AI.DAL.Models;
using CamAI.Service.AI.DAL.Interfaces;

using CamAI.Service.AI.BLL.Interfaces;

namespace CamAI.Service.AI.BLL.Services;

public class CameraService : BackgroundService, ICameraService
{
    private readonly IFaceDetector _detector;
    private readonly IFaceEmbedder _embedder;
    private readonly IFaceMatchService _matchService;
    private readonly IStreamProvider _streamProvider;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IMinioStorageService _minioStorageService;
    private readonly ICameraEventLogger _eventLogger;
    private readonly ILogger<CameraService> _logger;
    private readonly ICameraRepository _cameraRepo;
    private readonly IApiLogRepository _apiLogRepo;

    // === ANTI-SPAM LOGGING SYSTEM ===
    // Cooldown: Mỗi profileId chỉ ghi log 1 lần trong khoảng thời gian này
    private readonly Dictionary<Guid, DateTime> _lastLoggedPersons = new();
    // Cooldown cho người lạ: theo vùng không gian (tránh ghi trùng cùng 1 người lạ)
    private readonly Dictionary<string, DateTime> _lastLoggedUnknowns = new();
    // Bộ đếm xác nhận: Yêu cầu nhận diện đúng N lần liên tiếp trước khi ghi log
    private readonly Dictionary<string, int> _confirmationCounters = new();
    private readonly Dictionary<string, FaceRecognitionResult> _lastResults = new();

    // === MULTI-FACE TRACKER ===
    // Lưu vị trí các khuôn mặt đang theo dõi từ frame trước để ghép chính xác
    private readonly List<TrackedFace> _trackedFaces = new();
    private int _nextTrackId = 1;
    private const float IOU_MATCH_THRESHOLD = 0.25f; // IoU >= 0.25 = cùng 1 người
    private const int TRACK_TTL_FRAMES = 15;          // Mất dấu sau 15 frame AI (quãng ~75 frame thật)
    private DateTime _lastCleanupTime = DateTime.Now;

    // Cấu hình logging
    private const int MIN_FACE_SIZE = 80;          // Pixel tối thiểu (W hoặc H) để xử lý
    private const int CONFIRMATION_FRAMES = 7;      // Số frame AI liên tiếp phải nhất quán
    private const double KNOWN_COOLDOWN_MINUTES = 5; // Cooldown cho người quen
    private const double UNKNOWN_COOLDOWN_MINUTES = 2; // Cooldown cho người lạ
    private const double CLEANUP_INTERVAL_MINUTES = 10; // Dọn dẹp tracking data
    private const float UNKNOWN_MIN_CONFIDENCE = 0.85f; // YuNet confidence tối thiểu cho UNKNOWN
    private const float FRONTAL_RATIO_MIN = 0.20f;      // Mũi lệch < 20% khoảng cách 2 mắt mới là frontal
    private const float UNKNOWN_MAX_SIMILARITY = 0.20f; // Chỉ ghi log UNKNOWN khi similarity < 0.20 (người lạ thật sự)

    public CameraService(
        IFaceDetector detector,
        IFaceEmbedder embedder,
        IFaceMatchService matchService,
        IStreamProvider streamProvider,
        IEnrollmentService enrollmentService,
        IMinioStorageService minioStorageService,
        ICameraEventLogger eventLogger,
        ICameraRepository cameraRepo,
        IApiLogRepository apiLogRepo,
        ILogger<CameraService> logger)
    {
        _detector = detector;
        _embedder = embedder;
        _matchService = matchService;
        _streamProvider = streamProvider;
        _enrollmentService = enrollmentService;
        _minioStorageService = minioStorageService;
        _eventLogger = eventLogger;
        _cameraRepo = cameraRepo;
        _apiLogRepo = apiLogRepo;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khởi động Camera Service. Đang lấy cấu hình từ SQL qua API...");

        List<CameraConfig>? cameraConfigs = null;
        
        while (!stoppingToken.IsCancellationRequested && cameraConfigs == null)
        {
            try
            {
                cameraConfigs = await _cameraRepo.GetAllCamerasAsync(stoppingToken);
                if (cameraConfigs == null)
                {
                    _logger.LogWarning("Chưa thể lấy cấu hình Camera từ API. Thử lại sau 5s...");
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Lỗi khi lấy cấu hình Camera: {Msg}. Thử lại sau 5s...", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (cameraConfigs == null || !cameraConfigs.Any())
        {
            _logger.LogError("Khong tim thay cau hinh Camera nao trong co so du lieu!");
            return;
        }

        _logger.LogInformation("Da tai {Count} Camera tu SQL. Bat dau xu ly AI...", cameraConfigs.Count);

        var tasks = cameraConfigs.Select(config => RunCameraLogicAsync(config, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunCameraLogicAsync(CameraConfig config, CancellationToken stoppingToken)
    {
        _logger.LogInformation("[ID: {Id}] Bat dau: {Name} | Link: {Url} | Nguong: {Threshold}", 
            config.Id, config.CameraName, config.StreamUrl, config.RecognitionThreshold);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var capture = new VideoCapture();
                capture.Open(config.StreamUrl, VideoCaptureAPIs.FFMPEG);
                capture.Set(VideoCaptureProperties.BufferSize, 1); 

                using var frame = new Mat();

                if (!capture.IsOpened())
                {
                    _logger.LogWarning("[{Name}] Khong the ket noi. Thu lai sau 5s...", config.CameraName);
                    await _eventLogger.LogEventAsync("ERROR", $"Không thể kết nối Camera: {config.CameraName}", config.Id, config.CameraName);
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                await _eventLogger.LogEventAsync("CONNECTED", $"Camera {config.CameraName} đã kết nối thành công.", config.Id, config.CameraName);

                int frameCounter = 0;
                var latestDetections = new List<FaceDetectionResult>();
                var latestMatchResults = new Dictionary<int, FaceRecognitionResult>(); // Index -> Result

                while (!stoppingToken.IsCancellationRequested)
                {
                    int skipped = 0;
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (skipped < 100) 
                    {
                        var startTicks = sw.ElapsedTicks;
                        if (!capture.Grab()) break;
                        var endTicks = sw.ElapsedTicks;
                        skipped++;
                        if ((endTicks - startTicks) > (System.Diagnostics.Stopwatch.Frequency / 500)) 
                            break;
                    }

                    if (capture.Retrieve(frame) && !frame.Empty() && frame.Width > 0)
                    {
                        frameCounter++;
                        
                        // CHỈ CHẠY AI SAU MỖI 5 KHUNG HÌNH ĐỂ ĐẢM BẢO MƯỢT 30FPS
                        bool runAI = (frameCounter % 5 == 0);

                        try 
                        {
                            // COPY một frame riêng dành cho AI, để không làm biến dạng (nhấp nháy) frame hiển thị trên Stream
                            using Mat aiFrame = frame.Clone();

                            if (runAI)
                            {
                                // Aging: Tăng tuổi các track cũ, xóa track mất dấu
                                AgeTrackedFaces();

                                // Lọc nhiễu / Tăng nét trước khi nhận diện AI (chỉ áp dụng trên bản gốc của AI)
                                EnhanceImageQuality(aiFrame);

                                latestDetections = _detector.Detect(aiFrame);
                                latestMatchResults.Clear();

                                // Xử lý đăng ký khuôn mặt tự động (Nếu có yêu cầu)
                                var enrollReq = _enrollmentService.GetCurrentRequest();
                                if (enrollReq != null && (DateTime.Now - enrollReq.StartTime).TotalSeconds > 60)
                                {
                                    _enrollmentService.ClearRequest();
                                    enrollReq = null;
                                }

                                // NẾU ĐANG ĐĂNG KÝ: Chỉ lấy khuôn mặt LỚN NHẤT
                                List<FaceDetectionResult> facesToProcess = latestDetections;
                                if (enrollReq != null && latestDetections.Count > 1)
                                {
                                    var largestFace = latestDetections.OrderByDescending(f => f.Width * f.Height).First();
                                    facesToProcess = new List<FaceDetectionResult> { largestFace };
                                }

                                for (int i = 0; i < facesToProcess.Count; i++)
                                {
                                    var det = facesToProcess[i];
                                    var embedding = _embedder.GetEmbedding(aiFrame, det);

                                    if (enrollReq != null)
                                    {
                                        // KIỂM TRA XEM MẶT NÀY ĐÃ CÓ TRONG HỆ THỐNG CHƯA?
                                        var checkMatch = _matchService.Match(embedding, threshold: (float)config.RecognitionThreshold);
                                        if (checkMatch.IsKnown)
                                        {
                                            string plainKnownName = RemoveDiacritics(checkMatch.FullName);
                                            Cv2.PutText(frame, $"ALREADY REGISTERED: {plainKnownName.ToUpper()}", new Point(det.X, det.Y - 55), 
                                                HersheyFonts.HersheySimplex, 0.6, Scalar.Red, 2);
                                            // Vẫn vẽ khung đỏ để cảnh báo
                                            Cv2.Rectangle(frame, new Rect(det.X, det.Y, det.Width, det.Height), Scalar.Red, 2);
                                        }
                                        else
                                        {
                                            // CHỈ XỬ LÝ ĐĂNG KÝ CHO KHUÔN MẶT CHƯA BIẾT (V2 - Continuous)
                                            double angleDegree = GetFaceYaw(det.Landmarks);
                                            bool added = _enrollmentService.TryAddAngle(embedding, angleDegree, MatToBytes(frame), det.Confidence);
                                            
                                            if (added)
                                            {
                                                Cv2.PutText(frame, "SAVED NEW ANGLE", new Point(det.X, det.Y - 20), HersheyFonts.HersheySimplex, 0.7, Scalar.Green, 2);
                                            }

                                            if (enrollReq.IsComplete || enrollReq.IsFull)
                                            {
                                                string bucket = "faces";
                                                string datePath = DateTime.Now.ToString("yyyy/MM/dd");
                                                string personId = Guid.NewGuid().ToString();

                                                // Upload tất cả ảnh đã thu thập lên MinIO
                                                for (int j = 0; j < enrollReq.CapturedAngles.Count; j++)
                                                {
                                                    var capturedAngle = enrollReq.CapturedAngles[j];
                                                    string fileName = $"register/{datePath}/{personId}_{capturedAngle.AngleLabel}.jpg";
                                                    string url = await _minioStorageService.UploadFileAsync(bucket, fileName, new MemoryStream(capturedAngle.Image), "image/jpeg");
                                                    capturedAngle.MinioImageUrl = url;
                                                }

                                                // Gửi request API
                                                await _matchService.RegisterProfileV2Async(enrollReq);

                                                await _eventLogger.LogEventAsync("ENROLL_COMPLETE", $"Đã hoàn tất quét {enrollReq.CapturedAngles.Count} góc mặt cho: {enrollReq.FullName}", config.Id, config.CameraName);
                                                _enrollmentService.ClearRequest();
                                                enrollReq = null;
                                            }
                                        }
                                        
                                        // Sau khi xử lý đăng ký cho mặt lớn nhất, thoát vòng lặp
                                        break; 
                                    }
                                    else
                                    {
                                        // === LỌC MẶT QUÁ NHỎ (xa camera, mờ) ===
                                        if (det.Width < MIN_FACE_SIZE || det.Height < MIN_FACE_SIZE)
                                        {
                                            // Vẫn nhận diện để vẽ khung, nhưng KHÔNG ghi log
                                            var resultSmall = _matchService.Match(embedding, threshold: (float)config.RecognitionThreshold);
                                            latestMatchResults[i] = resultSmall;
                                            continue;
                                        }

                                        var threshold = (float)config.RecognitionThreshold;
                                        var result = _matchService.Match(embedding, threshold: threshold);
                                        latestMatchResults[i] = result;

                                        // === HỆ THỐNG XÁC NHẬN TRƯỚC KHI GHI LOG ===
                                        string trackingKey = GetTrackingKey(det, result);
                                        bool confirmed = UpdateConfirmation(trackingKey, result);

                                        if (!confirmed) continue; // Chưa đủ số frame xác nhận

                                        if (result.IsKnown && result.ProfileId.HasValue)
                                        {
                                            Guid profileId = result.ProfileId.Value;
                                            if (!_lastLoggedPersons.ContainsKey(profileId) || 
                                                (DateTime.Now - _lastLoggedPersons[profileId]).TotalMinutes > KNOWN_COOLDOWN_MINUTES)
                                            {
                                                _lastLoggedPersons[profileId] = DateTime.Now;
                                                _ = LogAccessAsync(config, result, det, frame.Clone());
                                            }
                                        }
                                        else if (!result.IsKnown)
                                        {
                                            // === VÙNG MƠ HỒ (AMBIGUOUS ZONE) ===
                                            // Similarity 0.20 - threshold: rất có thể là người quen ở góc xấu
                                            // Chỉ ghi log UNKNOWN khi similarity thực sự thấp (người lạ 100%)
                                            if (result.Similarity >= UNKNOWN_MAX_SIMILARITY)
                                            {
                                                _logger.LogDebug("[AmbiguousFilter] Bo qua UNKNOWN: similarity {Sim:F2} >= {Max:F2} (co the la nguoi quen goc xau)",
                                                    result.Similarity, UNKNOWN_MAX_SIMILARITY);
                                                continue;
                                            }

                                            // === LỌC CHẤT LƯỢNG MẶT ===
                                            if (det.Confidence < UNKNOWN_MIN_CONFIDENCE)
                                            {
                                                _logger.LogDebug("[QualityFilter] Bo qua UNKNOWN: confidence {Conf:F2} < {Min:F2}", det.Confidence, UNKNOWN_MIN_CONFIDENCE);
                                                continue;
                                            }

                                            if (det.Landmarks != null && !IsFaceFrontal(det.Landmarks))
                                            {
                                                _logger.LogDebug("[QualityFilter] Bo qua UNKNOWN: mat khong quay thang (landmark lech)");
                                                continue;
                                            }

                                            // Cooldown theo vùng không gian
                                            string areaKey = GetAreaKey(det);
                                            if (!_lastLoggedUnknowns.ContainsKey(areaKey) ||
                                                (DateTime.Now - _lastLoggedUnknowns[areaKey]).TotalMinutes > UNKNOWN_COOLDOWN_MINUTES)
                                            {
                                                _lastLoggedUnknowns[areaKey] = DateTime.Now;
                                                _ = LogAccessAsync(config, result, det, frame.Clone());
                                            }
                                        }

                                        // Dọn dẹp định kỳ
                                        CleanupTrackingData();
                                    }
                                }
                            }

                            // LƯU FRAME SẠCH (KHÔNG VẼ) ĐỂ HIỂN THỊ WEB
                            _streamProvider.SetLastRawFrame(MatToBytes(frame));

                            // VẼ LÊN FRAME (VẼ MỌI KHUNG HÌNH DỰA TRÊN KẾT QUẢ AI GẦN NHẤT)
                            var currentEnrollReq = _enrollmentService.GetCurrentRequest();
                            for (int i = 0; i < latestDetections.Count; i++)
                            {
                                var det = latestDetections[i];
                                var rect = new Rect(det.X, det.Y, det.Width, det.Height);
                                if (rect.Width <= 0 || rect.Height <= 0) continue;

                                if (currentEnrollReq != null)
                                {
                                    Cv2.Rectangle(frame, rect, Scalar.Yellow, 3);
                                    string prog = $"Angles: {currentEnrollReq.CapturedAngles.Count}/{currentEnrollReq.MinAnglesRequired} (Max {currentEnrollReq.MaxAngles})";
                                    Cv2.PutText(frame, "SCANNING...", new Point(rect.X, rect.Y - 35), HersheyFonts.HersheySimplex, 0.6, Scalar.Yellow, 2);
                                    Cv2.PutText(frame, prog, new Point(rect.X, rect.Y - 10), HersheyFonts.HersheySimplex, 0.5, Scalar.Cyan, 2);
                                }
                                else if (latestMatchResults.TryGetValue(i, out var result))
                                {
                                    var color = result.IsKnown ? Scalar.SpringGreen : Scalar.Red;
                                    var plainName = RemoveDiacritics(result.FullName);
                                    var label = $"{plainName} ({result.Similarity:P0})";
                                    Cv2.Rectangle(frame, rect, color, 3);
                                    Cv2.Rectangle(frame, new Rect(rect.X, Math.Max(0, rect.Y - 35), rect.Width, 35), color, -1);
                                    Cv2.PutText(frame, label, new Point(rect.X + 5, Math.Max(25, rect.Y - 10)), HersheyFonts.HersheySimplex, 0.7, Scalar.White, 2);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("[{Name}] Lỗi xử lý: {Msg}", config.CameraName, ex.Message);
                        }

                        // CẬP NHẬT FRAME CHO STREAM LIÊN TỤC
                        _streamProvider.SetLastFrame(MatToBytes(frame));
                    }
                    else if (skipped == 0) // Nếu không grab được frame nào (mất kết nối đột ngột)
                    {
                         _logger.LogWarning("[{Name}] Không nhận được dữ liệu từ Stream.", config.CameraName);
                         break;
                    }

                    // Delay nhỏ để tránh spam CPU nhưng vẫn đảm bảo gần thời gian thực nhất
                    await Task.Delay(5, stoppingToken); 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [{Name}] Lỗi luồng chính.", config.CameraName);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private string GetFaceAngle(Point2f[] landmarks)
    {
        if (landmarks == null || landmarks.Length < 5) return "unknown";

        float ratio = GetFaceYawRatio(landmarks);
        if (ratio == -1) return "unknown";

        if (ratio >= 0.7f && ratio <= 1.4f) return "front";
        if (ratio < 0.7f) return "left";
        return "right";
    }

    private float GetFaceYawRatio(Point2f[] landmarks)
    {
        if (landmarks == null || landmarks.Length < 5) return -1;
        float noseX = landmarks[2].X;
        float rightEyeX = landmarks[0].X;
        float leftEyeX = landmarks[1].X;

        float distR = Math.Abs(noseX - rightEyeX);
        float distL = Math.Abs(noseX - leftEyeX);

        if (distR == 0 || distL == 0) return -1;
        return distL / distR;
    }

    private double GetFaceYaw(Point2f[] landmarks)
    {
        if (landmarks == null || landmarks.Length < 5) return 0;
        float noseX = landmarks[2].X;
        float rightEyeX = landmarks[0].X; // Mắt phải của người (bên trái ảnh)
        float leftEyeX = landmarks[1].X;  // Mắt trái của người (bên phải ảnh)

        float distR = Math.Abs(noseX - rightEyeX);
        float distL = Math.Abs(noseX - leftEyeX);

        if (distR + distL == 0) return 0;

        // Xoay đầu: if nose is exactly in middle, distL = distR -> yaw = 0.
        // If nose is closer to right eye (distR small, distL large) -> person is looking to their right (our left) -> positive yaw.
        return 90.0 * (distL - distR) / (distL + distR);
    }

    /// <summary>
    /// Kiểm tra mặt có đang quay thẳng (frontal) hay không dựa trên 5 landmarks.
    /// Logic: Mũi (landmark[2]) phải nằm gần giữa đường nối 2 mắt (landmark[0] và landmark[1]).
    /// Nếu mặt nghiêng > ~30°, mũi sẽ lệch rõ sang 1 bên → return false.
    /// </summary>
    private bool IsFaceFrontal(Point2f[] landmarks)
    {
        if (landmarks == null || landmarks.Length < 5) return true; // Không có data → cho qua

        // landmark[0] = Mắt phải, landmark[1] = Mắt trái, landmark[2] = Mũi
        var rightEye = landmarks[0];
        var leftEye = landmarks[1];
        var nose = landmarks[2];

        // Tính điểm giữa 2 mắt
        float midX = (rightEye.X + leftEye.X) / 2f;
        float eyeDistance = Math.Abs(leftEye.X - rightEye.X);

        if (eyeDistance < 5) return false; // 2 mắt quá gần → mặt nghiêng cực đoan

        // Tỉ lệ lệch của mũi so với điểm giữa 2 mắt
        float noseOffset = Math.Abs(nose.X - midX);
        float offsetRatio = noseOffset / eyeDistance;

        // Mặt thẳng: offsetRatio < 0.25 (mũi gần giữa)
        // Mặt nghiêng 30°+: offsetRatio > 0.25
        return offsetRatio < FRONTAL_RATIO_MIN;
    }

    /// <summary>
    /// Tạo khóa theo dõi chính xác bằng Track ID thay vì grid.
    /// Sử dụng IoU matching để ghép khuôn mặt qua các frame.
    /// </summary>
    private string GetTrackingKey(FaceDetectionResult det, FaceRecognitionResult result)
    {
        int trackId = AssignTrackId(det, result);
        string identity = result.IsKnown && result.ProfileId.HasValue
            ? result.ProfileId.Value.ToString()
            : "unknown";
        return $"track_{trackId}_{identity}";
    }

    /// <summary>
    /// Gán Track ID cho detection dựa trên IoU với tracked faces từ frame trước.
    /// Nếu không khớp → tạo track mới. Nếu khớp → giữ nguyên track cũ.
    /// </summary>
    private int AssignTrackId(FaceDetectionResult det, FaceRecognitionResult result)
    {
        float bestIoU = 0f;
        TrackedFace? bestMatch = null;

        foreach (var tracked in _trackedFaces)
        {
            float iou = ComputeIoU(det, tracked);
            if (iou > bestIoU)
            {
                bestIoU = iou;
                bestMatch = tracked;
            }
        }

        if (bestMatch != null && bestIoU >= IOU_MATCH_THRESHOLD)
        {
            // Cập nhật vị trí mới nhất
            bestMatch.X = det.X;
            bestMatch.Y = det.Y;
            bestMatch.Width = det.Width;
            bestMatch.Height = det.Height;
            bestMatch.LastSeenFrame = 0; // Reset TTL
            bestMatch.LastIdentity = result.IsKnown && result.ProfileId.HasValue
                ? result.ProfileId.Value.ToString() : "unknown";
            return bestMatch.TrackId;
        }

        // Tạo track mới
        var newTrack = new TrackedFace
        {
            TrackId = _nextTrackId++,
            X = det.X,
            Y = det.Y,
            Width = det.Width,
            Height = det.Height,
            LastSeenFrame = 0,
            LastIdentity = result.IsKnown && result.ProfileId.HasValue
                ? result.ProfileId.Value.ToString() : "unknown"
        };
        _trackedFaces.Add(newTrack);
        return newTrack.TrackId;
    }

    /// <summary>
    /// Tính Intersection over Union (IoU) giữa detection hiện tại và tracked face.
    /// IoU = Diện tích giao / Diện tích hợp. Giá trị 0..1.
    /// </summary>
    private float ComputeIoU(FaceDetectionResult det, TrackedFace tracked)
    {
        int x1 = Math.Max(det.X, tracked.X);
        int y1 = Math.Max(det.Y, tracked.Y);
        int x2 = Math.Min(det.X + det.Width, tracked.X + tracked.Width);
        int y2 = Math.Min(det.Y + det.Height, tracked.Y + tracked.Height);

        int interW = Math.Max(0, x2 - x1);
        int interH = Math.Max(0, y2 - y1);
        float interArea = interW * interH;

        float areaA = det.Width * det.Height;
        float areaB = tracked.Width * tracked.Height;
        float unionArea = areaA + areaB - interArea;

        return unionArea > 0 ? interArea / unionArea : 0f;
    }

    /// <summary>
    /// Xử lý aging cho tracked faces: tăng tuổi và xóa track đã mất dấu.
    /// Gọi mỗi lần chạy AI frame.
    /// </summary>
    private void AgeTrackedFaces()
    {
        foreach (var t in _trackedFaces)
            t.LastSeenFrame++;

        _trackedFaces.RemoveAll(t => t.LastSeenFrame > TRACK_TTL_FRAMES);
    }

    /// <summary>
    /// Tạo khóa vùng không gian cho người lạ.
    /// Dùng lưới 60px thay vì 100px để chính xác hơn khi người đứng gần.
    /// </summary>
    private string GetAreaKey(FaceDetectionResult det)
    {
        return $"{det.X / 60}_{det.Y / 60}";
    }

    /// <summary>
    /// Hệ thống xác nhận: Yêu cầu nhận diện nhất quán qua N frame AI liên tiếp.
    /// Tránh ghi log do nhận nhầm 1 frame rồi frame sau đã đúng.
    /// </summary>
    private bool UpdateConfirmation(string trackingKey, FaceRecognitionResult currentResult)
    {
        if (_lastResults.TryGetValue(trackingKey, out var prevResult))
        {
            // So sánh kết quả: cùng người (hoặc cùng unknown) thì tăng counter
            bool sameResult = (currentResult.IsKnown == prevResult.IsKnown) &&
                              (currentResult.ProfileId == prevResult.ProfileId);
            if (sameResult)
            {
                _confirmationCounters[trackingKey] = _confirmationCounters.GetValueOrDefault(trackingKey, 0) + 1;
            }
            else
            {
                // Kết quả thay đổi → reset counter
                _confirmationCounters[trackingKey] = 1;
            }
        }
        else
        {
            _confirmationCounters[trackingKey] = 1;
        }

        _lastResults[trackingKey] = currentResult;
        return _confirmationCounters[trackingKey] >= CONFIRMATION_FRAMES;
    }

    /// <summary>
    /// Dọn dẹp các entry cũ hơn 10 phút để tránh memory leak trong service 24/7.
    /// </summary>
    private void CleanupTrackingData()
    {
        if ((DateTime.Now - _lastCleanupTime).TotalMinutes < CLEANUP_INTERVAL_MINUTES) return;
        _lastCleanupTime = DateTime.Now;

        var cutoff = DateTime.Now.AddMinutes(-CLEANUP_INTERVAL_MINUTES);

        // Dọn known persons
        var staleKnown = _lastLoggedPersons.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList();
        foreach (var key in staleKnown) _lastLoggedPersons.Remove(key);

        // Dọn unknown tracking
        var staleUnknown = _lastLoggedUnknowns.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList();
        foreach (var key in staleUnknown) _lastLoggedUnknowns.Remove(key);

        // Dọn confirmation counters (xóa hết vì data đã cũ)
        _confirmationCounters.Clear();
        _lastResults.Clear();

        // Dọn tracked faces cũ
        _trackedFaces.RemoveAll(t => t.LastSeenFrame > TRACK_TTL_FRAMES);

        if (staleKnown.Count + staleUnknown.Count > 0)
            _logger.LogDebug("[Cleanup] Da don {Known} known + {Unknown} unknown tracking entries.", staleKnown.Count, staleUnknown.Count);
    }

    private async Task LogAccessAsync(CameraConfig config, FaceRecognitionResult result, FaceDetectionResult det, Mat proofFrame)
    {
        try
        {
            using (proofFrame)
            {
                // Crop khuôn mặt để có hình ảnh rõ ràng hơn thay vì lấy toàn frame
                // Mở rộng bounding box ra khoảng 1.5 lần để lấy thêm vai/cổ
                int paddingX = (int)(det.Width * 0.25);
                int paddingY = (int)(det.Height * 0.25);
                
                int x = Math.Max(0, det.X - paddingX);
                int y = Math.Max(0, det.Y - paddingY);
                int w = Math.Min(proofFrame.Width - x, det.Width + paddingX * 2);
                int h = Math.Min(proofFrame.Height - y, det.Height + paddingY * 2);

                if (w <= 10 || h <= 10)
                {
                    _logger.LogWarning("[LogAccess] Vung crop qua nho ({W}x{H}), bo qua crop va lay full frame.", w, h);
                    await UploadAndLogAsync(config, result, proofFrame);
                    return;
                }

                using var croppedFace = new Mat(proofFrame, new Rect(x, y, w, h));
                await UploadAndLogAsync(config, result, croppedFace);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi ghi Access Log");
        }
    }

    private async Task UploadAndLogAsync(CameraConfig config, FaceRecognitionResult result, Mat image)
    {
        // 1. Upload ảnh bằng chứng lên MinIO
        string type = result.IsKnown ? "identified" : "alerts";
        string datePath = DateTime.Now.ToString("yyyy/MM/dd");
        string objectName = $"logs/{type}/{datePath}/{Guid.NewGuid()}.jpg";
        
        using var ms = new MemoryStream(MatToBytes(image));
        string imageUrl = await _minioStorageService.UploadFileAsync("faces", objectName, ms, "image/jpeg");

        // 2. Gửi Log về API
        string recognitionStatus = result.IsKnown ? "IDENTIFIED" : "UNKNOWN";

        var logRequest = new
        {
            ProfileId = result.ProfileId,
            FullName = result.FullName,
            MinioLogImage = imageUrl,
            DeviceImpacted = config.CameraName,
            RecognitionStatus = recognitionStatus,
            ConfidenceScore = result.Similarity
        };

        await _apiLogRepo.LogAccessAsync(logRequest);
        _logger.LogInformation("[LogAccess] Da ghi nhat ky cho {Name} tu camera {Cam}", result.FullName, config.CameraName);
    }

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var c in normalizedString)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                stringBuilder.Append(c);
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
    }

    private byte[] MatToBytes(Mat mat)
    {
        if (mat == null || mat.Empty()) return Array.Empty<byte>();
        // Sử dụng chất lượng 80 để cân bằng giữa độ nét và tốc độ
        var parameters = new int[] { (int)ImwriteFlags.JpegQuality, 80 };
        Cv2.ImEncode(".jpg", mat, out byte[] bytes, parameters);
        return bytes;
    }

    /// <summary>
    /// Hàm tiền xử lý ảnh (Làm nét & Cân bằng sáng) giúp AI nhìn rõ ở khoảng cách xa / thiếu sáng.
    /// Thời gian chạy ~5-15ms, rất phù hợp cho pipeline Real-time.
    /// </summary>
    private void EnhanceImageQuality(Mat frame)
    {
        if (frame == null || frame.Empty()) return;

        // 1. UNSHARP MASKING (Làm sắc nét các đường viền mờ do nhiễu/xa)
        // Tạo một bản sao làm mờ
        using var blurred = new Mat();
        Cv2.GaussianBlur(frame, blurred, new Size(0, 0), 2.0); // Sigma=2.0
        // frame = frame*1.5 - blurred*0.5 (Tăng tương phản cạnh)
        Cv2.AddWeighted(frame, 1.5, blurred, -0.5, 0, frame);

        // 2. CLAHE (Contrast Limited Adaptive Histogram Equalization - Cân bằng sáng cục bộ)
        // Rất hiệu quả cho vùng mặt bị ngược sáng hoặc tối đen.
        // Cần chuyển sang LAB color space để chỉ tác động lên kênh Sáng (L - Lightness)
        using var labFrame = new Mat();
        Cv2.CvtColor(frame, labFrame, ColorConversionCodes.BGR2Lab);
        
        // Split kênh L, A, B
        var labChannels = Cv2.Split(labFrame);
        if (labChannels.Length == 3)
        {
            using var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new Size(8, 8));
            clahe.Apply(labChannels[0], labChannels[0]);

            // Merge lại
            Cv2.Merge(labChannels, labFrame);
            // CvtColor back to BGR
            Cv2.CvtColor(labFrame, frame, ColorConversionCodes.Lab2BGR);
            
            // Giải phóng kênh
            foreach (var ch in labChannels) ch.Dispose();
        }
    }

    /// <summary>
    /// Đối tượng theo dõi khuôn mặt qua các frame bằng bounding box.
    /// </summary>
    private class TrackedFace
    {
        public int TrackId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int LastSeenFrame { get; set; }
        public string LastIdentity { get; set; } = "unknown";
    }
}
