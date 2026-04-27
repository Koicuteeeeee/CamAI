using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Interfaces;
using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Services;

public class FaceProfileService : IFaceProfileService
{
    private readonly IFaceProfileRepository _profileRepo;

    public FaceProfileService(IFaceProfileRepository profileRepo)
    {
        _profileRepo = profileRepo;
    }

    public async Task<IEnumerable<FaceProfileModel>> GetAllAsync()
        => await _profileRepo.GetAllAsync();

    public async Task<Guid> RegisterAsync(string fullName, float[] embeddingFront, float[] embeddingLeft, float[] embeddingRight, string minioFront, string minioLeft, string minioRight, string? externalCode = null, string? profileType = "Resident", string? createdBy = null)
    {
        byte[] frontBytes = EmbeddingToBytes(embeddingFront);
        byte[] leftBytes = EmbeddingToBytes(embeddingLeft);
        byte[] rightBytes = EmbeddingToBytes(embeddingRight);
        return await _profileRepo.RegisterAsync(fullName, externalCode, profileType, frontBytes, leftBytes, rightBytes, minioFront, minioLeft, minioRight, createdBy);
    }

    public async Task<List<FaceRecord>> GetAllFaceRecordsAsync()
    {
        var faces = await _profileRepo.GetAllFaceEmbeddingsAsync();
        return faces.Select(f => new FaceRecord
        {
            ProfileId = f.ProfileId,
            FullName = f.FullName,
            EmbeddingFront = BytesToEmbedding(f.EmbeddingFront),
            EmbeddingLeft = BytesToEmbedding(f.EmbeddingLeft),
            EmbeddingRight = BytesToEmbedding(f.EmbeddingRight),
            MinioFront = f.MinioFront,
            MinioLeft = f.MinioLeft,
            MinioRight = f.MinioRight
        }).ToList();
    }

    public async Task<bool> DeleteAsync(Guid profileId)
        => await _profileRepo.DeleteAsync(profileId);

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
