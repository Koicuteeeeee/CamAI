using System.IO;
using System.Threading.Tasks;

namespace CamAI.Common.Interfaces;

public interface IMinioStorageService
{
    Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType);
    Task<string> GetFileUrlAsync(string bucketName, string objectName);
}
