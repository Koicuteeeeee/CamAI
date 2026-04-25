using System.Net.Http.Json;
using CamAI.Common.Interfaces;
using CamAI.Common.Models;

namespace CamAI.Service.AI.Services;

/// <summary>
/// So khớp khuôn mặt, đọc/ghi dữ liệu thông qua CamAI.API thay vì lưu file cục bộ.
/// </summary>
public class ApiFaceMatchService : IFaceMatchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiFaceMatchService> _logger;
    private Dictionary<Guid, RegisteredFace> _registeredFaces = new();
    private readonly object _lock = new();

    public ApiFaceMatchService(HttpClient httpClient, ILogger<ApiFaceMatchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ = SyncWithApiAsync();
    }

    /// <summary>
    /// Đồng bộ dữ liệu vector từ CamAI.API
    /// </summary>
    public async Task SyncWithApiAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiFaceResponse>("api/users/faces");
            if (response != null && response.Success && response.Data != null)
            {
                lock (_lock)
                {
                    _registeredFaces.Clear();
                    foreach (var record in response.Data)
                    {
                        _registeredFaces[record.UserId] = new RegisteredFace
                        {
                            UserId = record.UserId,
                            FullName = record.FullName,
                            Embedding = record.Embedding
                        };
                    }
                }
                _logger.LogInformation("Đã đồng bộ {Count} khuôn mặt từ API.", _registeredFaces.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đồng bộ khuôn mặt từ API. Vui lòng kiểm tra CamAI.API đã chạy chưa.");
        }
    }

    public FaceRecognitionResult Match(float[] embedding, float threshold = 0.35f)
    {
        var result = new FaceRecognitionResult
        {
            IsKnown = false,
            UserId = Guid.Empty,
            FullName = "Người lạ",
            Similarity = 0f
        };

        if (embedding.Length == 0) return result;

        lock (_lock)
        {
            float maxSim = -1f;
            Guid bestUserId = Guid.Empty;
            string bestName = "";

            foreach (var face in _registeredFaces.Values)
            {
                float sim = CosineSimilarity(embedding, face.Embedding);
                if (sim > maxSim)
                {
                    maxSim = sim;
                    bestUserId = face.UserId;
                    bestName = face.FullName;
                }
            }

            if (maxSim >= threshold)
            {
                result.IsKnown = true;
                result.UserId = bestUserId;
                result.FullName = bestName;
                result.Similarity = maxSim;
            }
            else
            {
                result.Similarity = maxSim;
                _logger.LogWarning("Phát hiện người lạ! Gần giống nhất: {Name}, Sim = {Sim:F3}, Threshold = {Thres}", bestName, maxSim, threshold);
            }
        }
        return result;
    }

    public void Register(RegisteredFace face)
    {
        // Gửi lệnh đăng ký tới API
        try
        {
            var request = new
            {
                Username = face.FullName.ToLower().Replace(" ", ""),
                FullName = face.FullName,
                Embedding = face.Embedding,
                MinioObjectName = face.MinioObjectName
            };

            var postTask = _httpClient.PostAsJsonAsync("api/users/register", request);
            postTask.Wait(); // Chạy đồng bộ tạm thời vì Interface không trả về Task

            if (postTask.Result.IsSuccessStatusCode)
            {
                _logger.LogInformation("Đã đăng ký thành công lên API: {Name}", face.FullName);
                _ = SyncWithApiAsync(); // Đồng bộ lại sau khi đăng ký
            }
            else
            {
                _logger.LogError("Lỗi khi đăng ký lên API: {Status}", postTask.Result.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể kết nối đến CamAI.API để đăng ký.");
        }
    }

    public List<RegisteredFace> GetAllRegistered()
    {
        lock (_lock)
        {
            _logger.LogInformation("GetAllRegistered called. Current dictionary size: {Count}", _registeredFaces.Count);
            return _registeredFaces.Values.ToList();
        }
    }

    public bool Remove(Guid userId)
    {
        try
        {
            var deleteTask = _httpClient.DeleteAsync($"api/users/{userId}");
            deleteTask.Wait();
            if (deleteTask.Result.IsSuccessStatusCode)
            {
                lock (_lock)
                {
                    _registeredFaces.Remove(userId);
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể xóa User trên API.");
        }
        return false;
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        float denominator = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denominator > 0 ? dot / denominator : 0;
    }

    // === Class parse JSON ===
    private class ApiFaceResponse
    {
        public bool Success { get; set; }
        public List<ApiFaceRecord> Data { get; set; } = new();
    }

    private class ApiFaceRecord
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
