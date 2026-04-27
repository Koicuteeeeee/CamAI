using System.Net.Http.Json;
using System.Text.Json;
using CamAI.Common.Models;
using CamAI.Service.AI.DAL.Interfaces;
using CamAI.Service.AI.DAL.Models;

namespace CamAI.Service.AI.DAL.Repositories;

public class FaceDataRepository : IFaceDataRepository
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public FaceDataRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<UserFaceRecord>> GetAllFaceEmbeddingsAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("CamAI_API");
        var response = await client.GetFromJsonAsync<ApiFaceResponse>("api/faceprofiles/faces", _jsonOptions, ct);
        return response?.Data ?? new List<UserFaceRecord>();
    }

    public async Task RegisterFaceAsync(RegisteredFace face, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("CamAI_API");
        await client.PostAsJsonAsync("api/faceprofiles/register", face, ct);
    }

    public async Task<List<FaceEmbeddingRecord>> GetAllFaceEmbeddingsV2Async(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("CamAI_API");
        var response = await client.GetFromJsonAsync<ApiFaceResponseV2>("api/faceprofiles/faces-v2", _jsonOptions, ct);
        return response?.Data ?? new List<FaceEmbeddingRecord>();
    }

    public async Task<Guid> RegisterProfileV2Async(string fullName, string? externalCode = null, string? profileType = "Resident", string? createdBy = null, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("CamAI_API");
        var req = new { FullName = fullName, ExternalCode = externalCode, ProfileType = profileType, CreatedBy = createdBy };
        var response = await client.PostAsJsonAsync("api/faceprofiles/register-v2", req, ct);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<RegisterApiResult>(_jsonOptions, ct);
        return content?.ProfileId ?? Guid.Empty;
    }

    public async Task AddEmbeddingAsync(Guid profileId, string angleLabel, double? angleDegree, float[] embedding, string? minioImageUrl, double? captureQuality, string? createdBy = null, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("CamAI_API");
        var req = new {
            AngleLabel = angleLabel,
            AngleDegree = angleDegree,
            Embedding = embedding,
            MinioImageUrl = minioImageUrl,
            CaptureQuality = captureQuality,
            CreatedBy = createdBy
        };
        var response = await client.PostAsJsonAsync($"api/faceprofiles/{profileId}/embeddings", req, ct);
        response.EnsureSuccessStatusCode();
    }

    private class ApiFaceResponseV2
    {
        public bool Success { get; set; }
        public List<FaceEmbeddingRecord> Data { get; set; } = new();
    }

    private class RegisterApiResult
    {
        public bool Success { get; set; }
        public Guid ProfileId { get; set; }
    }
}
