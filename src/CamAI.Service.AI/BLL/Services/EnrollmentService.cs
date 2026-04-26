using System.Collections.Concurrent;
using CamAI.Common.Models;
using Microsoft.Extensions.Logging;
using CamAI.Service.AI.DAL.Models;
using CamAI.Service.AI.BLL.Interfaces;

namespace CamAI.Service.AI.BLL.Services;

public class EnrollmentService : IEnrollmentService
{
    private EnrollmentRequest? _currentRequest;
    private readonly object _lock = new();
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        _logger = logger;
    }

    public void StartEnrollment(string fullName)
    {
        lock (_lock)
        {
            _currentRequest = new EnrollmentRequest
            {
                FullName = fullName,
                StartTime = DateTime.Now
            };
            _logger.LogInformation("Bắt đầu quá trình đăng ký khuôn mặt cho: {Name}", fullName);
        }
    }

    public EnrollmentRequest? GetCurrentRequest() => _currentRequest;

    public void ClearRequest()
    {
        lock (_lock)
        {
            _currentRequest = null;
        }
    }

    public bool UpdateEmbedding(float[] embedding, string angle, byte[] image)
    {
        lock (_lock)
        {
            if (_currentRequest == null) return false;

            bool updated = false;
            switch (angle.ToLower())
            {
                case "front":
                    if (_currentRequest.EmbeddingFront == null) { _currentRequest.EmbeddingFront = embedding; _currentRequest.ImageFront = image; updated = true; }
                    break;
                case "left":
                    if (_currentRequest.EmbeddingLeft == null) { _currentRequest.EmbeddingLeft = embedding; _currentRequest.ImageLeft = image; updated = true; }
                    break;
                case "right":
                    if (_currentRequest.EmbeddingRight == null) { _currentRequest.EmbeddingRight = embedding; _currentRequest.ImageRight = image; updated = true; }
                    break;
            }

            if (updated)
            {
                _logger.LogInformation("Đã thu thập góc {Angle} cho {Name}", angle, _currentRequest.FullName);
            }
            return updated;
        }
    }
}
