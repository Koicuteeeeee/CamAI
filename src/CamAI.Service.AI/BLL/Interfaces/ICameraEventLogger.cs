namespace CamAI.Service.AI.BLL.Interfaces;

public interface ICameraEventLogger
{
    Task LogEventAsync(string eventType, string description, Guid? cameraId = null, string? cameraName = null);
}
