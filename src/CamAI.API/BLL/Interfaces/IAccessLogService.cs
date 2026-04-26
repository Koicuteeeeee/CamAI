using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Interfaces;

public interface IAccessLogService
{
    Task LogAccessAsync(Guid? userId, string? minioLogImage = null, string? deviceImpacted = null, string? recognitionStatus = null, double? confidenceScore = null);
    Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page = 1, int pageSize = 20);
}
