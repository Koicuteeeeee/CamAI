namespace CamAI.Service.AI.DAL.Models;

public class EnrollmentRequest
{
    public string FullName { get; set; } = string.Empty;
    public float[]? EmbeddingFront { get; set; }
    public float[]? EmbeddingLeft { get; set; }
    public float[]? EmbeddingRight { get; set; }
    public byte[]? ImageFront { get; set; }
    public byte[]? ImageLeft { get; set; }
    public byte[]? ImageRight { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Now;
    
    public bool IsComplete => EmbeddingFront != null && EmbeddingLeft != null && EmbeddingRight != null &&
                              ImageFront != null && ImageLeft != null && ImageRight != null;
}
