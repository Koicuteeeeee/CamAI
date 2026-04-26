using CamAI.Common.Models;
using CamAI.Service.AI.DAL.Interfaces;
using CamAI.Service.AI.DAL.Models;
using CamAI.Service.AI.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace CamAI.Service.AI.BLL.Services;

/// <summary>
/// So khớp khuôn mặt, đọc/ghi dữ liệu thông qua Repository thay vì gọi HttpClient trực tiếp.
/// </summary>
public class ApiFaceMatchService : IFaceMatchService
{
    private readonly IFaceDataRepository _faceRepo;
    private readonly ILogger<ApiFaceMatchService> _logger;
    private Dictionary<Guid, RegisteredFace> _registeredFaces = new();
    private readonly object _lock = new();

    public ApiFaceMatchService(IFaceDataRepository faceRepo, ILogger<ApiFaceMatchService> logger)
    {
        _faceRepo = faceRepo;
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
            var data = await _faceRepo.GetAllFaceEmbeddingsAsync();
            if (data != null)
            {
                lock (_lock)
                {
                    _registeredFaces.Clear();
                    foreach (var record in data)
                    {
                        _registeredFaces[record.UserId] = new RegisteredFace
                        {
                            UserId = record.UserId,
                            FullName = record.FullName,
                            EmbeddingFront = record.EmbeddingFront,
                            EmbeddingLeft = record.EmbeddingLeft,
                            EmbeddingRight = record.EmbeddingRight,
                            MinioFront = record.MinioFront,
                            MinioLeft = record.MinioLeft,
                            MinioRight = record.MinioRight
                        };
                    }
                }
                _logger.LogInformation("[MatchService] Da dong bo {Count} khuon mat (3 goc) tu API.", _registeredFaces.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi dong bo khuon mat tu API.");
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
                // So khớp với cả 3 góc độ, lấy góc có độ tương đồng cao nhất
                float simFront = CosineSimilarity(embedding, face.EmbeddingFront);
                float simLeft = CosineSimilarity(embedding, face.EmbeddingLeft);
                float simRight = CosineSimilarity(embedding, face.EmbeddingRight);

                float bestSim = Math.Max(simFront, Math.Max(simLeft, simRight));

                if (bestSim > maxSim)
                {
                    maxSim = bestSim;
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
                // _logger.LogWarning("Phát hiện người lạ! Gần giống nhất: {Name}, Sim = {Sim:F3}", bestName, maxSim);
            }
        }
        return result;
    }

    public async Task RegisterAsync(RegisteredFace face)
    {
        try
        {
            await _faceRepo.RegisterFaceAsync(face);
            _logger.LogInformation("✅ [MatchService] Da dang ky thanh cong: {Name}", face.FullName);
            await SyncWithApiAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MatchService] Loi khi dang ky khuon mat.");
        }
    }

    public List<RegisteredFace> GetAllRegistered()
    {
        lock (_lock)
        {
            return _registeredFaces.Values.ToList();
        }
    }

    public bool Remove(Guid userId)
    {
        // TODO: Implement Delete in Repository if needed
        _logger.LogWarning("Remove not implemented via Repository yet.");
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
}
