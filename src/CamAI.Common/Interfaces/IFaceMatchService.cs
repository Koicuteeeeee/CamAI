using CamAI.Common.Models;

namespace CamAI.Common.Interfaces;

/// <summary>
/// So khớp embedding với cơ sở dữ liệu người quen.
/// </summary>
public interface IFaceMatchService
{
    /// <summary>
    /// So sánh một embedding với toàn bộ khuôn mặt đã đăng ký.
    /// </summary>
    /// <param name="embedding">Vector đặc trưng cần so khớp.</param>
    /// <param name="threshold">Ngưỡng tương đồng tối thiểu (mặc định 0.6).</param>
    /// <returns>Kết quả nhận diện.</returns>
    FaceRecognitionResult Match(float[] embedding, float threshold = 0.35f);

    /// <summary>
    /// Đăng ký khuôn mặt mới vào hệ thống.
    /// </summary>
    void Register(RegisteredFace face);

    /// <summary>
    /// Lấy danh sách tất cả khuôn mặt đã đăng ký.
    /// </summary>
    List<RegisteredFace> GetAllRegistered();

    /// <summary>
    /// Xóa khuôn mặt theo UserId.
    /// </summary>
    bool Remove(Guid userId);
}
