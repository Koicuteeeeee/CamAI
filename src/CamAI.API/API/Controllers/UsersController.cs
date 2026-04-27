using CamAI.API.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CamAI.API.API.Controllers;

[Authorize]
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
    /// Lấy thông tin người dùng đang đăng nhập dựa trên Keycloak ID (sub claim).
    /// GET /api/users/me
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var keycloakIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(keycloakIdStr))
            return Unauthorized(new { success = false, message = "Không tìm thấy định danh người dùng trong Token" });

        if (!Guid.TryParse(keycloakIdStr, out var keycloakId))
            return BadRequest(new { success = false, message = "Định danh người dùng không hợp lệ" });

        var users = await _userService.GetAllAsync();
        var currentUser = users.FirstOrDefault(u => u.KeycloakId == keycloakId);

        if (currentUser == null)
            return NotFound(new { success = false, message = "Không tìm thấy thông tin người dùng trong hệ thống SQL" });

        return Ok(new { success = true, user = currentUser });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(new { success = true, data = users });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
    {
        var newId = await _userService.RegisterAsync(
            request.Username, 
            request.Email, 
            request.FullName, 
            request.Role, 
            request.KeycloakId,
            request.CreatedBy
        );
        return Ok(new { success = true, userId = newId });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = "Không tìm thấy người dùng" });

        return Ok(new { success = true, message = "Đã vô hiệu hóa người dùng" });
    }
}

public class UserRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public Guid? KeycloakId { get; set; }
    public string? CreatedBy { get; set; }
}
