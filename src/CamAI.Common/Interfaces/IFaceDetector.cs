using CamAI.Common.Models;
using OpenCvSharp;

namespace CamAI.Common.Interfaces;

/// <summary>
/// Phát hiện khuôn mặt trong frame ảnh.
/// </summary>
public interface IFaceDetector : IDisposable
{
    /// <summary>
    /// Detect tất cả khuôn mặt trong một frame.
    /// </summary>
    List<FaceDetectionResult> Detect(Mat frame, float confidenceThreshold = 0.7f);
}
