using CamAI.API.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CamAI.API.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaceProfilesController : ControllerBase
{
    private readonly IFaceProfileService _profileService;

    public FaceProfilesController(IFaceProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var profiles = await _profileService.GetAllAsync();
        return Ok(new { success = true, data = profiles });
    }

    [HttpGet("faces")]
    public async Task<IActionResult> GetAllFaces()
    {
        var faces = await _profileService.GetAllFaceRecordsAsync();
        return Ok(new { success = true, count = faces.Count, data = faces });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] FaceProfileRegisterRequest request)
    {
        var newId = await _profileService.RegisterAsync(
            request.FullName, 
            request.EmbeddingFront, 
            request.EmbeddingLeft, 
            request.EmbeddingRight, 
            request.MinioFront,
            request.MinioLeft,
            request.MinioRight,
            request.ExternalCode,
            request.ProfileType,
            request.CreatedBy
        );
        return Ok(new { success = true, profileId = newId });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _profileService.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = "Không tìm thấy hồ sơ" });

        return Ok(new { success = true, message = "Đã xóa hồ sơ" });
    }
}

public class FaceProfileRegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string? ProfileType { get; set; }
    public float[] EmbeddingFront { get; set; } = Array.Empty<float>();
    public float[] EmbeddingLeft { get; set; } = Array.Empty<float>();
    public float[] EmbeddingRight { get; set; } = Array.Empty<float>();
    public string MinioFront { get; set; } = string.Empty;
    public string MinioLeft { get; set; } = string.Empty;
    public string MinioRight { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}
