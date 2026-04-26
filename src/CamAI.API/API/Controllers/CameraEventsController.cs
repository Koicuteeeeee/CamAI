using CamAI.API.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CamAI.API.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CameraEventsController : ControllerBase
{
    private readonly ICameraEventService _eventService;

    public CameraEventsController(ICameraEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpPost]
    public async Task<IActionResult> LogEvent([FromBody] CameraEventRequest request)
    {
        await _eventService.LogEventAsync(request.CameraId, request.CameraName, request.EventType, request.Description);
        return Ok(new { success = true });
    }
}

public class CameraEventRequest
{
    public Guid? CameraId { get; set; }
    public string? CameraName { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
