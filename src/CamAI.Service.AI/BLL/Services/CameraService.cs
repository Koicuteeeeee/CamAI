using CamAI.Common.Interfaces;
using CamAI.Common.Models;
using OpenCvSharp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Net.Http.Json;
using System.Text.Json;
using CamAI.Service.AI.BLL.Models;

namespace CamAI.Service.AI.BLL.Services;

public class CameraService : BackgroundService
{
    private readonly IFaceDetector _detector;
    private readonly IFaceEmbedder _embedder;
    private readonly IFaceMatchService _matchService;
    private readonly StreamProvider _streamProvider;
    private readonly ILogger<CameraService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CameraService(
        IFaceDetector detector,
        IFaceEmbedder embedder,
        IFaceMatchService matchService,
        StreamProvider streamProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<CameraService> logger)
    {
        _detector = detector;
        _embedder = embedder;
        _matchService = matchService;
        _streamProvider = streamProvider;
        _httpClientFactory = httpClientFactory;
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
                var client = _httpClientFactory.CreateClient("CamAI_API");
                var response = await client.GetFromJsonAsync<ApiResponse<List<CameraConfig>>>("api/cameras", _jsonOptions, stoppingToken);
                
                if (response != null && response.Success)
                {
                    cameraConfigs = response.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Chưa thể lấy cấu hình Camera từ API: {Msg}. Thử lại sau 5s...", ex.Message);
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
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (capture.Read(frame) && !frame.Empty() && frame.Width > 0)
                    {
                        try 
                        {
                            var detections = _detector.Detect(frame);
                            foreach (var det in detections)
                            {
                                var embedding = _embedder.GetEmbedding(frame, det);
                                var result = _matchService.Match(embedding, threshold: (float)config.RecognitionThreshold);

                                var color = result.IsKnown ? Scalar.SpringGreen : Scalar.Red;
                                var plainName = RemoveDiacritics(result.FullName);
                                var label = $"{plainName} ({result.Similarity:P0})";
                                
                                var rect = new Rect(det.X, det.Y, det.Width, det.Height);
                                if (rect.Width > 0 && rect.Height > 0)
                                {
                                    Cv2.Rectangle(frame, rect, color, 3);
                                    Cv2.Rectangle(frame, new Rect(rect.X, Math.Max(0, rect.Y - 35), rect.Width, 35), color, -1);
                                    Cv2.PutText(frame, label, new Point(rect.X + 5, Math.Max(25, rect.Y - 10)), 
                                        HersheyFonts.HersheySimplex, 0.8, Scalar.White, 2, LineTypes.AntiAlias);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("[{Name}] Lỗi xử lý frame: {Msg}", config.CameraName, ex.Message);
                        }

                        _streamProvider.SetLastFrame(frame.ToBytes(".jpg"));
                    }
                    await Task.Delay(33, stoppingToken); 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [{Name}] Lỗi luồng chính.", config.CameraName);
                await Task.Delay(5000, stoppingToken);
            }
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
}
