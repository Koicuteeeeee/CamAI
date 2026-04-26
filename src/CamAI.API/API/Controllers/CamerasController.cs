using CamAI.API.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CamAI.API.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CamerasController : ControllerBase
{
    private readonly ICameraService _cameraService;

    public CamerasController(ICameraService cameraService)
    {
        _cameraService = cameraService;
    }

    /// <summary>
    /// Lấy danh sách Camera đang hoạt động và cấu hình nhạy cảm.
    /// GET /api/cameras
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cameras = await _cameraService.GetAllActiveAsync();
        return Ok(new { success = true, data = cameras });
    }
}
