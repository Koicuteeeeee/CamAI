using CamAI.API.BLL.Interfaces;
using CamAI.API.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CamAI.API.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessLogsController : ControllerBase
{
    private readonly IAccessLogService _logService;
    private readonly AccessLogEventBus _eventBus;

    public AccessLogsController(IAccessLogService logService, AccessLogEventBus eventBus)
    {
        _logService = logService;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Lấy lịch sử nhận diện (phân trang).
    /// GET /api/accesslogs?page=1&pageSize=20
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var logs = await _logService.GetHistoryAsync(page, pageSize);
        return Ok(new { success = true, data = logs });
    }

    /// <summary>
    /// Stream event realtime khi có access log mới (SSE).
    /// GET /api/accesslogs/stream
    /// </summary>
    [AllowAnonymous]
    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var channel = _eventBus.Subscribe();
        try
        {
            await Response.WriteAsync("event: ready\ndata: connected\n\n", ct);
            await Response.Body.FlushAsync(ct);

            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                var payload = JsonSerializer.Serialize(evt);
                await Response.WriteAsync($"event: access-log\ndata: {payload}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected.
        }
        finally
        {
            _eventBus.Unsubscribe(channel);
        }
    }

    /// <summary>
    /// Ghi một bản ghi nhật ký (AI Engine gọi endpoint này).
    /// POST /api/accesslogs
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] AccessLogRequest request)
    {
        await _logService.LogAccessAsync(request.ProfileId, request.FullName, request.MinioLogImage, request.DeviceImpacted, request.RecognitionStatus, request.ConfidenceScore, request.CreatedBy);

        _eventBus.Publish(new AccessLogEvent(
            request.ProfileId,
            request.FullName,
            request.RecognitionStatus,
            request.ConfidenceScore,
            DateTime.UtcNow
        ));

        return Ok(new { success = true, message = "Đã ghi nhật ký" });
    }
}

public class AccessLogRequest
{
    public Guid? ProfileId { get; set; }
    public string? FullName { get; set; }
    public string? MinioLogImage { get; set; }
    public string? DeviceImpacted { get; set; }
    public string? RecognitionStatus { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? CreatedBy { get; set; }
}
