using CamAI.API.DAL.Models;

namespace CamAI.API.DAL.Interfaces;

public interface IAccessLogRepository
{
    Task InsertAsync(Guid? profileId, string? fullName, string? minioLogImage, string? deviceImpacted, string? recognitionStatus, double? confidenceScore, string? createdBy = null);
    Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page, int pageSize);
}
