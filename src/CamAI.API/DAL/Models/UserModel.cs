namespace CamAI.API.DAL.Models;

public class UserModel
{
    public Guid Id { get; set; }
    public Guid? KeycloakId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string Role { get; set; } = "Staff";
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
