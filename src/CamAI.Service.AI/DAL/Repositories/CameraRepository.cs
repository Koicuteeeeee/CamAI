using System.Net.Http.Json;
using System.Text.Json;
using CamAI.Service.AI.DAL.Interfaces;
using CamAI.Service.AI.DAL.Models;

namespace CamAI.Service.AI.DAL.Repositories;

public class CameraRepository : ICameraRepository
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CameraRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<CameraConfig>?> GetAllCamerasAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("CamAI_API");
        var response = await client.GetFromJsonAsync<ApiResponse<List<CameraConfig>>>("api/cameras", _jsonOptions, ct);
        return response?.Data;
    }
}
