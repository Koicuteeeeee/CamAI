namespace CamAI.API.DAL.Models;

public class UserFaceModel
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MinioFront { get; set; } = string.Empty;
    public string MinioLeft { get; set; } = string.Empty;
    public string MinioRight { get; set; } = string.Empty;
    public byte[] EmbeddingFront { get; set; } = Array.Empty<byte>();
    public byte[] EmbeddingLeft { get; set; } = Array.Empty<byte>();
    public byte[] EmbeddingRight { get; set; } = Array.Empty<byte>();
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
