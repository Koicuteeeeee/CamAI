namespace CamAI.API.DAL.Models;

public class UserFaceModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MinioObjectName { get; set; } = string.Empty;
    public byte[] FaceEmbedding { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
}
