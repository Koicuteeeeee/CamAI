using CamAI.API.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CamAI.API.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessLogsController : ControllerBase
{
    private readonly IAccessLogService _logService;

    public AccessLogsController(IAccessLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// Lấy lịch sử nhận diện (phân trang).
    /// GET /api/accesslogs?page=1&pageSize=20
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var logs = await _logService.GetHistoryAsync(page, pageSize);
        return Ok(new { success = true, data = logs });
    }

    /// <summary>
    /// Ghi một bản ghi nhật ký (AI Engine gọi endpoint này).
    /// POST /api/accesslogs
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] AccessLogRequest request)
    {
        await _logService.LogAccessAsync(request.UserId, request.MinioLogImage, request.DeviceImpacted, request.RecognitionStatus, request.ConfidenceScore);
        return Ok(new { success = true, message = "Đã ghi nhật ký" });
    }
}

public class AccessLogRequest
{
    public Guid? UserId { get; set; }
    public string? MinioLogImage { get; set; }
    public string? DeviceImpacted { get; set; }
    public string? RecognitionStatus { get; set; }
    public double? ConfidenceScore { get; set; }
}
