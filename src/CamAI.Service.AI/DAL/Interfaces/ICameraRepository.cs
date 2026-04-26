using CamAI.Service.AI.DAL.Models;

namespace CamAI.Service.AI.DAL.Interfaces;

public interface ICameraRepository
{
    Task<List<CameraConfig>?> GetAllCamerasAsync(CancellationToken ct = default);
}
