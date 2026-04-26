namespace CamAI.Service.AI.DAL.Interfaces;

public interface IApiLogRepository
{
    Task LogAccessAsync(object logRequest, CancellationToken ct = default);
    Task LogCameraEventAsync(object eventRequest, CancellationToken ct = default);
}
