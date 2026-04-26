using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserModel>> GetAllActiveAsync();
    Task<UserModel?> GetByIdAsync(Guid id);
    Task<Guid> RegisterAsync(string username, string fullName, float[] embeddingFront, float[] embeddingLeft, float[] embeddingRight, string minioFront, string minioLeft, string minioRight);
    Task<List<FaceRecord>> GetAllFaceRecordsAsync();
    Task<bool> DeleteAsync(Guid userId);
}

/// <summary>
/// DTO chứa Embedding dạng float[] để AI Engine sử dụng trực tiếp.
/// </summary>
public class FaceRecord
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public float[] EmbeddingFront { get; set; } = Array.Empty<float>();
    public float[] EmbeddingLeft { get; set; } = Array.Empty<float>();
    public float[] EmbeddingRight { get; set; } = Array.Empty<float>();
    public string MinioFront { get; set; } = string.Empty;
    public string MinioLeft { get; set; } = string.Empty;
    public string MinioRight { get; set; } = string.Empty;
}
