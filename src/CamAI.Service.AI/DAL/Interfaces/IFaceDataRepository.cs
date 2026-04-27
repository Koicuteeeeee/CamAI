using CamAI.Service.AI.DAL.Models;
using CamAI.Common.Models;

namespace CamAI.Service.AI.DAL.Interfaces;

public interface IFaceDataRepository
{
    // V1 (backward compatible)
    Task<List<UserFaceRecord>> GetAllFaceEmbeddingsAsync(CancellationToken ct = default);
    Task RegisterFaceAsync(RegisteredFace face, CancellationToken ct = default);

    // V2 (đa góc độ)
    Task<List<FaceEmbeddingRecord>> GetAllFaceEmbeddingsV2Async(CancellationToken ct = default);
    Task<Guid> RegisterProfileV2Async(string fullName, string? externalCode = null, string? profileType = "Resident", string? createdBy = null, CancellationToken ct = default);
    Task AddEmbeddingAsync(Guid profileId, string angleLabel, double? angleDegree, float[] embedding, string? minioImageUrl, double? captureQuality, string? createdBy = null, CancellationToken ct = default);
}
