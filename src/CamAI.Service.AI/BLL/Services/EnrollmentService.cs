using System.Collections.Concurrent;
using CamAI.Common.Models;
using Microsoft.Extensions.Logging;
using CamAI.Service.AI.DAL.Models;
using CamAI.Service.AI.BLL.Interfaces;

namespace CamAI.Service.AI.BLL.Services;

public class EnrollmentService : IEnrollmentService
{
    private EnrollmentRequestV2? _currentRequest;
    private readonly object _lock = new();
    private readonly ILogger<EnrollmentService> _logger;
    private const double MIN_ANGLE_DIFF = 15.0; // Ít nhất 15 độ chênh lệch để coi là góc mới

    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        _logger = logger;
    }

    public void StartEnrollment(string fullName, int minAnglesRequired = 5, int maxAngles = 10)
    {
        lock (_lock)
        {
            _currentRequest = new EnrollmentRequestV2
            {
                FullName = fullName,
                StartTime = DateTime.Now,
                MinAnglesRequired = minAnglesRequired,
                MaxAngles = maxAngles
            };
            _logger.LogInformation("Bắt đầu đăng ký liên tục (Max {Max}) cho: {Name}", maxAngles, fullName);
        }
    }

    public EnrollmentRequestV2? GetCurrentRequest() => _currentRequest;

    public void ClearRequest()
    {
        lock (_lock)
        {
            _currentRequest = null;
        }
    }

    public bool TryAddAngle(float[] embedding, double angleDegree, byte[] image, float quality)
    {
        lock (_lock)
        {
            if (_currentRequest == null || _currentRequest.IsFull) return false;

            // Kiểm tra xem góc này đã trùng với góc nào chưa (cách nhau >= 15 độ)
            bool isNewAngle = true;
            foreach (var captured in _currentRequest.CapturedAngles)
            {
                if (Math.Abs(captured.AngleDegree - angleDegree) < MIN_ANGLE_DIFF)
                {
                    // Nếu trùng góc nhưng chất lượng (quality/confidence) tốt hơn thì ghi đè
                    if (quality > captured.Quality)
                    {
                        captured.Embedding = embedding;
                        captured.Image = image;
                        captured.Quality = quality;
                        _logger.LogInformation("Cập nhật lại góc {Deg} độ vì nháy được ảnh nét hơn ({Q})", (int)angleDegree, quality);
                        return true; 
                    }
                    isNewAngle = false;
                    break;
                }
            }

            if (isNewAngle)
            {
                _currentRequest.CapturedAngles.Add(new CapturedAngle
                {
                    AngleLabel = $"angle_{(int)angleDegree}",
                    AngleDegree = angleDegree,
                    Embedding = embedding,
                    Image = image,
                    Quality = quality
                });
                _logger.LogInformation("Đã thêm góc mới {AngleLabel} ({Q}). Tổng góc: {C}/{Max}", $"angle_{(int)angleDegree}", quality, _currentRequest.CapturedAngles.Count, _currentRequest.MaxAngles);
                return true;
            }

            return false;
        }
    }
}
