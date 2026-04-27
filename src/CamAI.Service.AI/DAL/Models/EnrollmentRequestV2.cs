namespace CamAI.Service.AI.DAL.Models;

/// <summary>
/// Yêu cầu đăng ký khuôn mặt V2: Thu thập liên tục N góc thay vì 3 góc cố định.
/// AI sẽ tự phát hiện góc mới và thêm vào danh sách.
/// </summary>
public class EnrollmentRequestV2
{
    public string FullName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.Now;

    /// <summary>Số góc tối thiểu để hoàn tất (mặc định 5)</summary>
    public int MinAnglesRequired { get; set; } = 5;

    /// <summary>Số góc tối đa thu thập (tránh quá nhiều)</summary>
    public int MaxAngles { get; set; } = 10;

    /// <summary>Danh sách các góc đã thu thập</summary>
    public List<CapturedAngle> CapturedAngles { get; set; } = new();

    /// <summary>Đã thu thập đủ góc tối thiểu chưa</summary>
    public bool IsComplete => CapturedAngles.Count >= MinAnglesRequired;

    /// <summary>Đã đạt giới hạn tối đa chưa</summary>
    public bool IsFull => CapturedAngles.Count >= MaxAngles;
}

public class CapturedAngle
{
    public string AngleLabel { get; set; } = string.Empty;  // "front", "left_30", "right_45", "up_20", "down_15", ...
    public double AngleDegree { get; set; }                  // Góc ước lượng (từ landmark)
    public float[] Embedding { get; set; } = [];
    public byte[] Image { get; set; } = [];
    public float Quality { get; set; }                       // YuNet confidence
    public string MinioImageUrl { get; set; } = string.Empty;
}
