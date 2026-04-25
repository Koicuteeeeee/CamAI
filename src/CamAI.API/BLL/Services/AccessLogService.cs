using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Models;
using CamAI.API.DAL.Repository;

namespace CamAI.API.BLL.Services;

public class AccessLogService : IAccessLogService
{
    private readonly AccessLogRepository _logRepo;

    public AccessLogService(AccessLogRepository logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task LogAccessAsync(Guid? userId, string actionTaken, string? minioLogImage = null, string? deviceImpacted = null)
        => await _logRepo.InsertAsync(userId, actionTaken, minioLogImage, deviceImpacted);

    public async Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page = 1, int pageSize = 20)
        => await _logRepo.GetHistoryAsync(page, pageSize);
}
