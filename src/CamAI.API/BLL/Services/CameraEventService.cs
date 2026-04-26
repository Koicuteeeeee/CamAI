using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Interfaces;

namespace CamAI.API.BLL.Services;

public class CameraEventService : ICameraEventService
{
    private readonly ICameraEventRepository _repo;

    public CameraEventService(ICameraEventRepository repo)
    {
        _repo = repo;
    }

    public async Task LogEventAsync(Guid? cameraId, string? cameraName, string eventType, string description)
    {
        await _repo.InsertAsync(cameraId, cameraName, eventType, description);
    }
}
