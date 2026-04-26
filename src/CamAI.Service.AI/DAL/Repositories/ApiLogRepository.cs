using System.Net.Http.Json;
using CamAI.Service.AI.DAL.Interfaces;

namespace CamAI.Service.AI.DAL.Repositories;

public class ApiLogRepository : IApiLogRepository
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiLogRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task LogAccessAsync(object logRequest, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("CamAI_API");
        await client.PostAsJsonAsync("api/accesslogs", logRequest, ct);
    }

    public async Task LogCameraEventAsync(object eventRequest, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("CamAI_API");
        await client.PostAsJsonAsync("api/cameraevents", eventRequest, ct);
    }
}
