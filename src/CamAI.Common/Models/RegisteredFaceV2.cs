namespace CamAI.Common.Models;

/// <summary>
/// Dữ liệu khuôn mặt đã đăng ký trong hệ thống (V2 - Đa góc độ).
/// Mỗi RegisteredFaceV2 chứa N embedding thay vì 3 cố định.
/// </summary>
public class RegisteredFaceV2
{
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;

    /// <summary>Danh sách N embedding đa góc độ.</summary>
    public List<FaceAngleEmbedding> Embeddings { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Một embedding đơn lẻ ứng với 1 góc mặt cụ thể.
/// </summary>
public class FaceAngleEmbedding
{
    public Guid Id { get; set; }
    public string AngleLabel { get; set; } = string.Empty;   // "front", "left", "right", "up", "down", ...
    public double? AngleDegree { get; set; }                  // Góc ước lượng (độ), nullable
    public float[] Embedding { get; set; } = [];               // Vector 128-dim
    public string MinioImageUrl { get; set; } = string.Empty;  // Ảnh trên MinIO
    public double CaptureQuality { get; set; }                 // Confidence YuNet (0..1)
}
