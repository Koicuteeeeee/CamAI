using OpenCvSharp;

namespace CamAI.Common.Interfaces;

/// <summary>
/// Trích xuất vector đặc trưng (embedding) từ khuôn mặt đã crop.
/// </summary>
public interface IFaceEmbedder : IDisposable
{
    /// <summary>
    /// Tạo embedding từ ảnh khuôn mặt đã được crop.
    /// </summary>
    /// <param name="faceImage">Ảnh khuôn mặt (đã crop từ frame gốc).</param>
    /// <returns>Vector đặc trưng (thường 128 hoặc 512 chiều).</returns>
    float[] GetEmbedding(Mat faceImage);

    /// <summary>
    /// Tạo embedding có sử dụng Face Alignment dựa trên landmarks.
    /// </summary>
    float[] GetEmbedding(Mat frame, Models.FaceDetectionResult detection);
}
