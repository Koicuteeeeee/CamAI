namespace CamAI.Common.Models;

/// <summary>
/// Kết quả nhận diện khuôn mặt - cho biết người này là ai.
/// </summary>
public class FaceRecognitionResult
{
    /// <summary>Có phải người quen không.</summary>
    public bool IsKnown { get; set; }

    /// <summary>ID người dùng (null nếu là người lạ).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Tên hiển thị.</summary>
    public string FullName { get; set; } = "Người lạ";

    /// <summary>Độ tương đồng (0.0 - 1.0). Càng cao càng giống.</summary>
    public float Similarity { get; set; }

    /// <summary>Vùng mặt trong ảnh.</summary>
    public FaceDetectionResult FaceRegion { get; set; } = new();
}
