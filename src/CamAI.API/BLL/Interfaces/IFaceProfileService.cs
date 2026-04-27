using CamAI.API.DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamAI.API.BLL.Interfaces;

public interface IFaceProfileService
{
    Task<IEnumerable<FaceProfileModel>> GetAllAsync();
    Task<Guid> RegisterAsync(string fullName, float[] embeddingFront, float[] embeddingLeft, float[] embeddingRight, string minioFront, string minioLeft, string minioRight, string? externalCode = null, string? profileType = "Resident", string? createdBy = null);
    Task<List<FaceRecord>> GetAllFaceRecordsAsync();
    Task<bool> DeleteAsync(Guid profileId);
}

public class FaceRecord
{
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public float[] EmbeddingFront { get; set; } = Array.Empty<float>();
    public float[] EmbeddingLeft { get; set; } = Array.Empty<float>();
    public float[] EmbeddingRight { get; set; } = Array.Empty<float>();
    public string MinioFront { get; set; } = string.Empty;
    public string MinioLeft { get; set; } = string.Empty;
    public string MinioRight { get; set; } = string.Empty;
}
