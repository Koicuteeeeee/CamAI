namespace CamAI.Common.Models;

/// <summary>
/// Dữ liệu khuôn mặt đã đăng ký trong hệ thống.
/// </summary>
public class RegisteredFace
{
    /// <summary>ID người dùng.</summary>
    public Guid UserId { get; set; }

    /// <summary>Tên đầy đủ.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Vector đặc trưng khuôn mặt (trực diện).</summary>
    public float[] EmbeddingFront { get; set; } = [];

    /// <summary>Vector đặc trưng khuôn mặt (nghiêng trái).</summary>
    public float[] EmbeddingLeft { get; set; } = [];

    /// <summary>Vector đặc trưng khuôn mặt (nghiêng phải).</summary>
    public float[] EmbeddingRight { get; set; } = [];

    /// <summary>Đường dẫn tệp ảnh trực diện trên MinIO.</summary>
    public string MinioFront { get; set; } = string.Empty;

    /// <summary>Đường dẫn tệp ảnh nghiêng trái trên MinIO.</summary>
    public string MinioLeft { get; set; } = string.Empty;

    /// <summary>Đường dẫn tệp ảnh nghiêng phải trên MinIO.</summary>
    public string MinioRight { get; set; } = string.Empty;
}
