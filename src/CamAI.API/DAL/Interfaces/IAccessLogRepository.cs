using CamAI.API.DAL.Models;

namespace CamAI.API.DAL.Interfaces;

public interface IAccessLogRepository
{
    Task InsertAsync(Guid? userId, string? minioLogImage, string? deviceImpacted, string? recognitionStatus, double? confidenceScore);
    Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page, int pageSize);
}
