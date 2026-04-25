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

    /// <summary>Vector đặc trưng khuôn mặt (128 hoặc 512 chiều tùy model).</summary>
    public float[] Embedding { get; set; } = [];

    /// <summary>Đường dẫn lưu trên MinIO.</summary>
    public string MinioObjectName { get; set; } = string.Empty;
}
