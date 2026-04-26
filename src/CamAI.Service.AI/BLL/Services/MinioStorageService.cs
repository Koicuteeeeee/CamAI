using CamAI.Service.AI.BLL.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace CamAI.Service.AI.BLL.Services;

public class MinioStorageService : IMinioStorageService
{
    private readonly IMinioClient _minioClient;

    public MinioStorageService(IConfiguration configuration)
    {
        var endpoint = configuration["Minio:Endpoint"];
        var accessKey = configuration["Minio:AccessKey"];
        var secretKey = configuration["Minio:SecretKey"];

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public async Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType)
    {
        // Kiểm tra xem Bucket đã tồn tại chưa, nếu chưa thì tạo mới (Cực kỳ an toàn)
        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
        if (!found)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
        }

        // Upload file
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs);

        // Trả về tên file để lưu vào Database SQL
        return objectName;
    }

    public async Task<string> GetFileUrlAsync(string bucketName, string objectName)
    {
        // Lấy Presigned URL (Link có thời hạn) để có thể xem nhanh trên Web Browser
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpiry(60 * 60 * 24); // Link sống trong vòng 24 giờ
            
        return await _minioClient.PresignedGetObjectAsync(args);
    }
}
