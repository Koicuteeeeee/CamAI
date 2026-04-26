using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Interfaces;

public interface ICameraEventService
{
    Task LogEventAsync(Guid? cameraId, string? cameraName, string eventType, string description);
}
