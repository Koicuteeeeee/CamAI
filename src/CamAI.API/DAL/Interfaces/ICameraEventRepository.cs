namespace CamAI.API.DAL.Interfaces;

public interface ICameraEventRepository
{
    Task InsertAsync(Guid? cameraId, string? cameraName, string eventType, string description, string? createdBy = null);
}
