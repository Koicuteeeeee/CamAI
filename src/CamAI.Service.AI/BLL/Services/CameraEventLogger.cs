using CamAI.Service.AI.BLL.Interfaces;
using CamAI.Service.AI.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace CamAI.Service.AI.BLL.Services;

public class CameraEventLogger : ICameraEventLogger
{
    private readonly IApiLogRepository _logRepo;
    private readonly ILogger<CameraEventLogger> _logger;

    public CameraEventLogger(IApiLogRepository logRepo, ILogger<CameraEventLogger> logger)
    {
        _logRepo = logRepo;
        _logger = logger;
    }

    public async Task LogEventAsync(string eventType, string description, Guid? cameraId = null, string? cameraName = null)
    {
        try
        {
            var request = new
            {
                CameraId = cameraId,
                CameraName = cameraName,
                EventType = eventType,
                Description = description
            };

            await _logRepo.LogCameraEventAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi CameraEvent");
        }
    }
}
