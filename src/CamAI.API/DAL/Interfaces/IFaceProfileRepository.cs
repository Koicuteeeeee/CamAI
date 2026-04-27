using CamAI.API.DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamAI.API.DAL.Interfaces;

public interface IFaceProfileRepository
{
    Task<IEnumerable<FaceProfileModel>> GetAllAsync();
    Task<bool> DeleteAsync(Guid profileId);

    // V2 (N góc độ)
    Task<IEnumerable<FaceEmbeddingModel>> GetAllFaceEmbeddingsV2Async();
    Task<Guid> RegisterProfileV2Async(string fullName, string? externalCode = null, string? profileType = "Resident", string? createdBy = null);
    Task<Guid> AddEmbeddingAsync(Guid profileId, string angleLabel, float? angleDegree, byte[] embedding, string? minioImageUrl, float? captureQuality, string? createdBy = null);
}
