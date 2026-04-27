using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Interfaces;

public interface IAccessLogService
{
    Task LogAccessAsync(Guid? profileId, string? fullName = null, string? minioLogImage = null, string? deviceImpacted = null, string? recognitionStatus = null, double? confidenceScore = null, string? createdBy = null);
    Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page = 1, int pageSize = 20);
}
