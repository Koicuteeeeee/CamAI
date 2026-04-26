using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Interfaces;
using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<UserModel>> GetAllActiveAsync()
        => await _userRepo.GetAllActiveAsync();

    public async Task<UserModel?> GetByIdAsync(Guid id)
        => await _userRepo.GetByIdAsync(id);

    /// <summary>
    /// Đăng ký User mới. Chuyển float[] embedding sang byte[] để lưu DB.
    /// </summary>
    public async Task<Guid> RegisterAsync(string username, string fullName, float[] embeddingFront, float[] embeddingLeft, float[] embeddingRight, string minioFront, string minioLeft, string minioRight)
    {
        byte[] frontBytes = EmbeddingToBytes(embeddingFront);
        byte[] leftBytes = EmbeddingToBytes(embeddingLeft);
        byte[] rightBytes = EmbeddingToBytes(embeddingRight);
        return await _userRepo.RegisterAsync(username, fullName, frontBytes, leftBytes, rightBytes, minioFront, minioLeft, minioRight);
    }

    /// <summary>
    /// Lấy tất cả Face Records (đã convert byte[] -> float[]) cho AI Engine.
    /// </summary>
    public async Task<List<FaceRecord>> GetAllFaceRecordsAsync()
    {
        var faces = await _userRepo.GetAllFaceEmbeddingsAsync();
        return faces.Select(f => new FaceRecord
        {
            UserId = f.UserId,
            FullName = f.FullName,
            EmbeddingFront = BytesToEmbedding(f.EmbeddingFront),
            EmbeddingLeft = BytesToEmbedding(f.EmbeddingLeft),
            EmbeddingRight = BytesToEmbedding(f.EmbeddingRight),
            MinioFront = f.MinioFront,
            MinioLeft = f.MinioLeft,
            MinioRight = f.MinioRight
        }).ToList();
    }

    public async Task<bool> DeleteAsync(Guid userId)
        => await _userRepo.DeleteAsync(userId);

    // === Utility: float[] <-> byte[] ===

    private static byte[] EmbeddingToBytes(float[] embedding)
    {
        byte[] bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToEmbedding(byte[] bytes)
    {
        float[] embedding = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
        return embedding;
    }
}
