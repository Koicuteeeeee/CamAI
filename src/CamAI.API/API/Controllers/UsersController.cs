using CamAI.API.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CamAI.API.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Lấy danh sách User đang hoạt động.
    /// GET /api/users
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllActiveAsync();
        return Ok(new { success = true, data = users });
    }

    /// <summary>
    /// Lấy User theo ID.
    /// GET /api/users/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { success = false, message = $"Không tìm thấy User ID: {id}" });

        return Ok(new { success = true, data = user });
    }

    /// <summary>
    /// Lấy tất cả Face Embeddings (AI Engine gọi endpoint này để so khớp).
    /// GET /api/users/faces
    /// </summary>
    [HttpGet("faces")]
    public async Task<IActionResult> GetAllFaces()
    {
        var faces = await _userService.GetAllFaceRecordsAsync();
        return Ok(new { success = true, count = faces.Count, data = faces });
    }

    /// <summary>
    /// Lưu User mới cùng Vector khuôn mặt (AI Engine gọi sau khi trích xuất vector).
    /// POST /api/users/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Nếu không có Username, dùng FullName (không dấu) + Guid ngắn
        string username = string.IsNullOrWhiteSpace(request.Username) 
            ? Guid.NewGuid().ToString("N").Substring(0, 8) 
            : request.Username;

        var newId = await _userService.RegisterAsync(
            username, 
            request.FullName, 
            request.EmbeddingFront, 
            request.EmbeddingLeft, 
            request.EmbeddingRight, 
            request.MinioFront,
            request.MinioLeft,
            request.MinioRight
        );
        return Ok(new { success = true, userId = newId });
    }

    /// <summary>
    /// Xóa User.
    /// DELETE /api/users/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = $"Không tìm thấy User ID: {id}" });

        return Ok(new { success = true, message = $"Đã xóa User ID: {id}" });
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public float[] EmbeddingFront { get; set; } = Array.Empty<float>();
    public float[] EmbeddingLeft { get; set; } = Array.Empty<float>();
    public float[] EmbeddingRight { get; set; } = Array.Empty<float>();
    public string MinioFront { get; set; } = string.Empty;
    public string MinioLeft { get; set; } = string.Empty;
    public string MinioRight { get; set; } = string.Empty;
}
