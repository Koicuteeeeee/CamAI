namespace CamAI.API.DAL.Models;

public class FaceEmbeddingModel
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AngleLabel { get; set; } = string.Empty;
    public double? AngleDegree { get; set; }
    public byte[] Embedding { get; set; } = Array.Empty<byte>();
    public string? MinioImageUrl { get; set; }
    public double? CaptureQuality { get; set; }
}
