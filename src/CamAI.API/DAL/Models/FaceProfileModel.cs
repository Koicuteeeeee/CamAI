namespace CamAI.API.DAL.Models;

public class FaceProfileModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string? ProfileType { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
