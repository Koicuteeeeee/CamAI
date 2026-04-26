using CamAI.Common.Models;
using CamAI.Service.AI.BLL.Interfaces;
using OpenCvSharp;

namespace CamAI.Service.AI.BLL.Services;

/// <summary>
/// Phát hiện khuôn mặt bằng YuNet của OpenCV.
/// </summary>
public class YuNetFaceDetector : IFaceDetector
{
    private FaceDetectorYN _detector;
    private readonly ILogger<YuNetFaceDetector> _logger;
    private readonly string _modelPath;
    private int _currentWidth = 0;
    private int _currentHeight = 0;

    public YuNetFaceDetector(string modelPath, ILogger<YuNetFaceDetector> logger)
    {
        _logger = logger;
        _modelPath = modelPath;
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model YuNet không tìm thấy: {modelPath}");

        _logger.LogInformation("YuNet FaceDetector loaded: {Path}", modelPath);
    }

    private float _currentConfidenceThreshold = 0;

    public List<FaceDetectionResult> Detect(Mat frame, float confidenceThreshold = 0.4f)
    {
        var results = new List<FaceDetectionResult>();
        if (frame.Empty()) return results;

        // Giảm maxSize xuống 720 để tăng tốc độ xử lý, tránh trễ luồng
        int maxSize = 720;
        float scale = 1.0f;
        Mat detectFrame = frame;

        if (frame.Width > maxSize || frame.Height > maxSize)
        {
            scale = Math.Min((float)maxSize / frame.Width, (float)maxSize / frame.Height);
            detectFrame = new Mat();
            Cv2.Resize(frame, detectFrame, new Size((int)(frame.Width * scale), (int)(frame.Height * scale)), 0, 0, InterpolationFlags.Area);
        }

        if (_detector == null || _currentWidth != detectFrame.Width || _currentHeight != detectFrame.Height || _currentConfidenceThreshold != confidenceThreshold)
        {
            _detector?.Dispose();
            // YuNet 2023: scoreThreshold=0.4, nmsThreshold=0.3, topK=5000 là tối ưu cho nhiều góc độ
            _detector = FaceDetectorYN.Create(_modelPath, "", new Size(detectFrame.Width, detectFrame.Height), confidenceThreshold, 0.3f, 5000);
            _currentWidth = detectFrame.Width;
            _currentHeight = detectFrame.Height;
            _currentConfidenceThreshold = confidenceThreshold;
        }

        using var faces = new Mat();
        _detector.Detect(detectFrame, faces);
        
        // Giải phóng ảnh tạm nếu đã resize
        if (detectFrame != frame) detectFrame.Dispose();

        if (faces.Empty() || faces.Rows == 0 || faces.Cols < 15) return results;

        for (int i = 0; i < faces.Rows; i++)
        {
            float confidence = faces.At<float>(i, 14);
            if (confidence < confidenceThreshold) continue;

            int x = (int)(faces.At<float>(i, 0) / scale);
            int y = (int)(faces.At<float>(i, 1) / scale);
            int w = (int)(faces.At<float>(i, 2) / scale);
            int h = (int)(faces.At<float>(i, 3) / scale);

            // landmarks (5 điểm: x,y của mắt phải, mắt trái, mũi, miệng phải, miệng trái)
            var landmarks = new Point2f[5];
            for (int j = 0; j < 5; j++)
            {
                landmarks[j] = new Point2f(
                    faces.At<float>(i, 4 + j * 2) / scale,
                    faces.At<float>(i, 5 + j * 2) / scale
                );
            }

            // Bảm bảo không nằm ngoài ảnh
            x = Math.Max(0, x);
            y = Math.Max(0, y);
            w = Math.Min(w, frame.Width - x);
            h = Math.Min(h, frame.Height - y);

            if (w > 10 && h > 10)
            {
                results.Add(new FaceDetectionResult
                {
                    X = x,
                    Y = y,
                    Width = w,
                    Height = h,
                    Confidence = confidence,
                    Landmarks = landmarks
                });
            }
        }

        return results;
    }

    public void Dispose()
    {
        _detector?.Dispose();
        GC.SuppressFinalize(this);
    }
}
