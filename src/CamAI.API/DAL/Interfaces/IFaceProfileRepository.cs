using CamAI.API.DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamAI.API.DAL.Interfaces;

public interface IFaceProfileRepository
{
    Task<IEnumerable<FaceProfileModel>> GetAllAsync();
    Task<Guid> RegisterAsync(string fullName, string? externalCode, string? profileType, byte[] embeddingFront, byte[] embeddingLeft, byte[] embeddingRight, string minioFront, string minioLeft, string minioRight, string? createdBy = null);
    Task<IEnumerable<UserFaceModel>> GetAllFaceEmbeddingsAsync();
    Task<bool> DeleteAsync(Guid profileId);
}
