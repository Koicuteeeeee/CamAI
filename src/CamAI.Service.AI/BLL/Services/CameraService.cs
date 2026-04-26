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

    // Theo dõi thời gian ghi log gần nhất của từng User (Để tránh spam log)
    private readonly Dictionary<Guid, DateTime> _lastLoggedPersons = new();
    private DateTime _lastLoggedUnknown = DateTime.MinValue;

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
                            if (runAI)
                            {
                                latestDetections = _detector.Detect(frame);
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
                                    var embedding = _embedder.GetEmbedding(frame, det);

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
                                            // CHỈ XỬ LÝ ĐĂNG KÝ CHO KHUÔN MẶT CHƯA BIẾT
                                            string angle = GetFaceAngle(det.Landmarks);
                                            _enrollmentService.UpdateEmbedding(embedding, angle, MatToBytes(frame));
                                            
                                            if (enrollReq.IsComplete)
                                            {
                                                string bucket = "faces";
                                                string personId = Guid.NewGuid().ToString();
                                                string datePath = DateTime.Now.ToString("yyyy/MM/dd");

                                                // Upload 3 ảnh theo cấu trúc Năm/Tháng/Ngày
                                                string urlFront = await _minioStorageService.UploadFileAsync(bucket, $"register/{datePath}/{personId}_front.jpg", new MemoryStream(enrollReq.ImageFront!), "image/jpeg");
                                                string urlLeft = await _minioStorageService.UploadFileAsync(bucket, $"register/{datePath}/{personId}_left.jpg", new MemoryStream(enrollReq.ImageLeft!), "image/jpeg");
                                                string urlRight = await _minioStorageService.UploadFileAsync(bucket, $"register/{datePath}/{personId}_right.jpg", new MemoryStream(enrollReq.ImageRight!), "image/jpeg");

                                                await _matchService.RegisterAsync(new RegisteredFace
                                                {
                                                    FullName = enrollReq.FullName,
                                                    EmbeddingFront = enrollReq.EmbeddingFront!,
                                                    EmbeddingLeft = enrollReq.EmbeddingLeft!,
                                                    EmbeddingRight = enrollReq.EmbeddingRight!,
                                                    MinioFront = urlFront,
                                                    MinioLeft = urlLeft,
                                                    MinioRight = urlRight
                                                });

                                                await _eventLogger.LogEventAsync("ENROLL_COMPLETE", $"Đã hoàn tất quét mặt cho: {enrollReq.FullName}", config.Id, config.CameraName);
                                                _enrollmentService.ClearRequest();
                                                enrollReq = null;
                                            }
                                        }
                                        
                                        // Sau khi xử lý đăng ký cho mặt lớn nhất, thoát vòng lặp
                                        break; 
                                    }
                                    else
                                    {
                                        var result = _matchService.Match(embedding, threshold: (float)config.RecognitionThreshold);
                                        latestMatchResults[i] = result;
                                        // ... (giữ nguyên logic log access)
                                        if (result.IsKnown && result.UserId.HasValue)
                                        {
                                            Guid userId = result.UserId.Value;
                                            if (!_lastLoggedPersons.ContainsKey(userId) || (DateTime.Now - _lastLoggedPersons[userId]).TotalMinutes > 5)
                                            {
                                                _lastLoggedPersons[userId] = DateTime.Now;
                                                _ = LogAccessAsync(config, result, frame.Clone());
                                            }
                                        }
                                        else if (!result.IsKnown)
                                        {
                                            if ((DateTime.Now - _lastLoggedUnknown).TotalMinutes > 5)
                                            {
                                                _lastLoggedUnknown = DateTime.Now;
                                                _ = LogAccessAsync(config, result, frame.Clone());
                                            }
                                        }
                                    }
                                }
                            }

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
                                    string prog = $"[{ (currentEnrollReq.EmbeddingFront != null ? "V" : " ") }] F  " +
                                                 $"[{ (currentEnrollReq.EmbeddingLeft != null ? "V" : " ") }] L  " +
                                                 $"[{ (currentEnrollReq.EmbeddingRight != null ? "V" : " ") }] R";
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

        // landmarks: 0=RE (v.left), 1=LE (v.right), 2=Nose, 3=RM, 4=LM
        float noseX = landmarks[2].X;
        float rightEyeX = landmarks[0].X;
        float leftEyeX = landmarks[1].X;

        float distR = Math.Abs(noseX - rightEyeX);
        float distL = Math.Abs(noseX - leftEyeX);

        if (distR == 0 || distL == 0) return "unknown";

        float ratio = distL / distR;

        // Ước lượng góc dựa trên tỷ lệ mũi-mắt
        if (ratio >= 0.7f && ratio <= 1.4f) return "front";
        if (ratio < 0.7f) return "left"; // Mũi gần mắt trái hơn (nhìn về bên trái của họ)
        if (ratio > 1.4f) return "right"; // Mũi gần mắt phải hơn

        return "unknown";
    }

    private async Task LogAccessAsync(CameraConfig config, FaceRecognitionResult result, Mat proofFrame)
    {
        try
        {
            using (proofFrame)
            {
                // 1. Upload ảnh bằng chứng lên MinIO (faces/logs/{type}/yyyy/MM/dd/...)
                string type = result.IsKnown ? "identified" : "alerts";
                string datePath = DateTime.Now.ToString("yyyy/MM/dd");
                string objectName = $"logs/{type}/{datePath}/{Guid.NewGuid()}.jpg";
                
                using var ms = new MemoryStream(MatToBytes(proofFrame));
                string imageUrl = await _minioStorageService.UploadFileAsync("faces", objectName, ms, "image/jpeg");

                // 2. Gửi Log về API
                string recognitionStatus = result.IsKnown ? "IDENTIFIED" : "UNKNOWN";

                var logRequest = new
                {
                    UserId = result.UserId,
                    MinioLogImage = imageUrl,
                    DeviceImpacted = config.CameraName,
                    RecognitionStatus = recognitionStatus,
                    ConfidenceScore = result.Similarity
                };

                await _apiLogRepo.LogAccessAsync(logRequest);
                
                _logger.LogInformation("[LogAccess] Da ghi nhat ky cho {Name} tu camera {Cam}", result.FullName, config.CameraName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi ghi Access Log");
        }
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
}
