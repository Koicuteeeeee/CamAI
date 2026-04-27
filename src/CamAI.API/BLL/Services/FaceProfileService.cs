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
        byte[] frontBytes = EmbeddingToBytes(embeddingFront);
        byte[] leftBytes = EmbeddingToBytes(embeddingLeft);
        byte[] rightBytes = EmbeddingToBytes(embeddingRight);
        return await _profileRepo.RegisterAsync(fullName, externalCode, profileType, frontBytes, leftBytes, rightBytes, minioFront, minioLeft, minioRight, createdBy);
    }

    public async Task<List<FaceRecord>> GetAllFaceRecordsAsync()
    {
        var faces = await _profileRepo.GetAllFaceEmbeddingsAsync();
        var records = faces.Select(f => new FaceRecord
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

        foreach (var record in records)
        {
            record.MinioFront = await ToPresignedUrlOrOriginalAsync(record.MinioFront);
            record.MinioLeft = await ToPresignedUrlOrOriginalAsync(record.MinioLeft);
            record.MinioRight = await ToPresignedUrlOrOriginalAsync(record.MinioRight);
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
