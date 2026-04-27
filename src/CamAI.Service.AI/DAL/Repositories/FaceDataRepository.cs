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
}
