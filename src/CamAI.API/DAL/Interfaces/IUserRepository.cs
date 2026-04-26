using CamAI.API.DAL.Models;

namespace CamAI.API.DAL.Interfaces;

public interface IUserRepository
{
    Task<IEnumerable<UserModel>> GetAllActiveAsync();
    Task<UserModel?> GetByIdAsync(Guid id);
    Task<Guid> RegisterAsync(string username, string fullName, byte[] embeddingFront, byte[] embeddingLeft, byte[] embeddingRight, string minioFront, string minioLeft, string minioRight);
    Task<IEnumerable<UserFaceModel>> GetAllFaceEmbeddingsAsync();
    Task<bool> DeleteAsync(Guid userId);
}
