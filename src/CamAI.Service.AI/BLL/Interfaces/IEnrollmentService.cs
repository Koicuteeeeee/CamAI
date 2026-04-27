using CamAI.Service.AI.DAL.Models;

namespace CamAI.Service.AI.BLL.Interfaces;

public interface IEnrollmentService
{
    void StartEnrollment(string fullName, int minAnglesRequired = 5, int maxAngles = 10);
    EnrollmentRequestV2? GetCurrentRequest();
    void ClearRequest();
    
    /// <summary>
    /// Thêm góc mặt tự động nếu đủ khác biệt so với các góc đã lưu.
    /// Giúp đăng ký N góc mà người dùng chỉ việc tự do xoay quanh.
    /// </summary>
    bool TryAddAngle(float[] embedding, double angleDegree, byte[] image, float quality);
}
