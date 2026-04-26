namespace CamAI.Service.AI.BLL.Interfaces;

public interface IMinioStorageService
{
    Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType);
    Task<string> GetFileUrlAsync(string bucketName, string objectName);
}
