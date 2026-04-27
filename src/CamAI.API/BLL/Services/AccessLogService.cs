using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Interfaces;
using CamAI.API.DAL.Models;
using Minio;
using Minio.DataModel.Args;

namespace CamAI.API.BLL.Services;

public class AccessLogService : IAccessLogService
{
    private readonly IAccessLogRepository _logRepo;
    private readonly IMinioClient _minioClient;
    private const string FacesBucket = "faces";

    public AccessLogService(IAccessLogRepository logRepo, IConfiguration configuration)
    {
        _logRepo = logRepo;

        var endpoint = configuration["Minio:Endpoint"] ?? "localhost:9000";
        var accessKey = configuration["Minio:AccessKey"] ?? "admin";
        var secretKey = configuration["Minio:SecretKey"] ?? "password123";

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public async Task LogAccessAsync(Guid? profileId, string? fullName = null, string? minioLogImage = null, string? deviceImpacted = null, string? recognitionStatus = null, double? confidenceScore = null, string? createdBy = null)
        => await _logRepo.InsertAsync(profileId, fullName, minioLogImage, deviceImpacted, recognitionStatus, confidenceScore, createdBy);

    public async Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page = 1, int pageSize = 20)
    {
        var logs = (await _logRepo.GetHistoryAsync(page, pageSize)).ToList();

        foreach (var log in logs)
        {
            if (string.IsNullOrWhiteSpace(log.MinioLogImage))
            {
                continue;
            }

            if (Uri.TryCreate(log.MinioLogImage, UriKind.Absolute, out _))
            {
                continue;
            }

            var objectName = NormalizeObjectName(log.MinioLogImage);
            try
            {
                var presignedUrl = await _minioClient.PresignedGetObjectAsync(
                    new PresignedGetObjectArgs()
                        .WithBucket(FacesBucket)
                        .WithObject(objectName)
                        .WithExpiry(60 * 60)
                );

                log.MinioLogImage = presignedUrl;
            }
            catch
            {
                // Giữ nguyên object key cũ để frontend fallback nếu cần.
            }
        }

        return logs;
    }

    private static string NormalizeObjectName(string objectName)
    {
        var normalized = objectName.Trim().TrimStart('/');
        return normalized.StartsWith("faces/", StringComparison.OrdinalIgnoreCase)
            ? normalized["faces/".Length..]
            : normalized;
    }
}
