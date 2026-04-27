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

    // V2
    Task<List<FaceEmbeddingRecordDto>> GetAllFaceEmbeddingsV2Async();
    Task<Guid> RegisterProfileV2Async(string fullName, string? externalCode = null, string? profileType = "Resident", string? createdBy = null);
    Task<Guid> AddEmbeddingAsync(Guid profileId, string angleLabel, float? angleDegree, float[] embedding, string? minioImageUrl, float? captureQuality, string? createdBy = null);
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

public class FaceEmbeddingRecordDto
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AngleLabel { get; set; } = string.Empty;
    public double? AngleDegree { get; set; }
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string? MinioImageUrl { get; set; }
    public double? CaptureQuality { get; set; }
}
