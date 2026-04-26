using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Interfaces;
using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Services;

public class AccessLogService : IAccessLogService
{
    private readonly IAccessLogRepository _logRepo;

    public AccessLogService(IAccessLogRepository logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task LogAccessAsync(Guid? userId, string? minioLogImage = null, string? deviceImpacted = null, string? recognitionStatus = null, double? confidenceScore = null)
        => await _logRepo.InsertAsync(userId, minioLogImage, deviceImpacted, recognitionStatus, confidenceScore);

    public async Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page = 1, int pageSize = 20)
        => await _logRepo.GetHistoryAsync(page, pageSize);
}
