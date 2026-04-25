using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserModel>> GetAllActiveAsync();
    Task<UserModel?> GetByIdAsync(Guid id);
    Task<Guid> RegisterAsync(string username, string fullName, float[] embedding, string minioObjectName);
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
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string MinioObjectName { get; set; } = string.Empty;
}
