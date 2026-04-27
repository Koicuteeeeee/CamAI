using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Interfaces;
using CamAI.API.DAL.Models;
using Minio;
using Minio.DataModel.Args;

namespace CamAI.API.BLL.Services;

public class FaceProfileService : IFaceProfileService
{
    private readonly IFaceProfileRepository _profileRepo;
    private readonly IMinioClient _minioClient;
    private const string FacesBucket = "faces";

    public FaceProfileService(IFaceProfileRepository profileRepo, IConfiguration configuration)
    {
        _profileRepo = profileRepo;

        var endpoint = configuration["Minio:Endpoint"] ?? "localhost:9000";
        var accessKey = configuration["Minio:AccessKey"] ?? "admin";
        var secretKey = configuration["Minio:SecretKey"] ?? "password123";

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public async Task<IEnumerable<FaceProfileModel>> GetAllAsync()
        => await _profileRepo.GetAllAsync();

    public async Task<Guid> RegisterAsync(string fullName, float[] embeddingFront, float[] embeddingLeft, float[] embeddingRight, string minioFront, string minioLeft, string minioRight, string? externalCode = null, string? profileType = "Resident", string? createdBy = null)
    {
        // V1 Compatibility: Chuyển hướng sang quy trình đăng ký V2
        var profileId = await RegisterProfileV2Async(fullName, externalCode, profileType, createdBy);
        
        await AddEmbeddingAsync(profileId, "front", 0, embeddingFront, minioFront, 1.0f, createdBy);
        await AddEmbeddingAsync(profileId, "left", -30, embeddingLeft, minioLeft, 1.0f, createdBy);
        await AddEmbeddingAsync(profileId, "right", 30, embeddingRight, minioRight, 1.0f, createdBy);
        
        return profileId;
    }

    public async Task<List<FaceRecord>> GetAllFaceRecordsAsync()
    {
        // V1 Compatibility: Lấy từ V2 và map về cấu trúc 3 góc (lấy 3 bản ghi đầu tiên nếu có)
        var v2Data = await GetAllFaceEmbeddingsV2Async();
        var grouped = v2Data.GroupBy(v => v.ProfileId);
        
        var records = new List<FaceRecord>();
        foreach (var group in grouped)
        {
            var pFirst = group.First();
            var angles = group.ToList();
            
            records.Add(new FaceRecord
            {
                ProfileId = group.Key,
                FullName = pFirst.FullName,
                EmbeddingFront = angles.Count > 0 ? angles[0].Embedding : Array.Empty<float>(),
                EmbeddingLeft = angles.Count > 1 ? angles[1].Embedding : Array.Empty<float>(),
                EmbeddingRight = angles.Count > 2 ? angles[2].Embedding : Array.Empty<float>(),
                MinioFront = angles.Count > 0 ? angles[0].MinioImageUrl ?? "" : "",
                MinioLeft = angles.Count > 1 ? angles[1].MinioImageUrl ?? "" : "",
                MinioRight = angles.Count > 2 ? angles[2].MinioImageUrl ?? "" : ""
            });
        }

        return records;
    }

    public async Task<List<FaceEmbeddingRecordDto>> GetAllFaceEmbeddingsV2Async()
    {
        var records = await _profileRepo.GetAllFaceEmbeddingsV2Async();
        var dtos = records.Select(r => new FaceEmbeddingRecordDto
        {
            Id = r.Id,
            ProfileId = r.ProfileId,
            FullName = r.FullName,
            AngleLabel = r.AngleLabel,
            AngleDegree = r.AngleDegree,
            Embedding = BytesToEmbedding(r.Embedding),
            MinioImageUrl = r.MinioImageUrl,
            CaptureQuality = r.CaptureQuality
        }).ToList();

        foreach (var dto in dtos)
        {
            if (!string.IsNullOrEmpty(dto.MinioImageUrl))
                dto.MinioImageUrl = await ToPresignedUrlOrOriginalAsync(dto.MinioImageUrl);
        }

        return dtos;
    }

    public async Task<Guid> RegisterProfileV2Async(string fullName, string? externalCode = null, string? profileType = "Resident", string? createdBy = null)
    {
        return await _profileRepo.RegisterProfileV2Async(fullName, externalCode, profileType, createdBy);
    }

    public async Task<Guid> AddEmbeddingAsync(Guid profileId, string angleLabel, float? angleDegree, float[] embedding, string? minioImageUrl, float? captureQuality, string? createdBy = null)
    {
        byte[] embBytes = EmbeddingToBytes(embedding);
        return await _profileRepo.AddEmbeddingAsync(profileId, angleLabel, angleDegree, embBytes, minioImageUrl, captureQuality, createdBy);
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

    private async Task<string> ToPresignedUrlOrOriginalAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return value;
        }

        var objectName = value.Trim().TrimStart('/');
        if (objectName.StartsWith("faces/", StringComparison.OrdinalIgnoreCase))
        {
            objectName = objectName["faces/".Length..];
        }

        try
        {
            return await _minioClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(FacesBucket)
                    .WithObject(objectName)
                    .WithExpiry(60 * 60)
            );
        }
        catch
        {
            return value;
        }
    }
}
