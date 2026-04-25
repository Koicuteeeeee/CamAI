namespace CamAI.Common.Models;

/// <summary>
/// Kết quả phát hiện khuôn mặt trong một frame ảnh.
/// </summary>
public class FaceDetectionResult
{
    /// <summary>Tọa độ X góc trái trên.</summary>
    public int X { get; set; }

    /// <summary>Tọa độ Y góc trái trên.</summary>
    public int Y { get; set; }

    /// <summary>Chiều rộng vùng mặt.</summary>
    public int Width { get; set; }

    /// <summary>Chiều cao vùng mặt.</summary>
    public int Height { get; set; }

    /// <summary>Độ tin cậy (0.0 - 1.0).</summary>
    public float Confidence { get; set; }

    // Thêm điểm đặc trưng (5 Landmarks: Mắt phải, mắt trái, mũi, mép phải, mép trái)
    public OpenCvSharp.Point2f[] Landmarks { get; set; } = new OpenCvSharp.Point2f[5];
}
