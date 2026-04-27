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
    private readonly PeriodicTimer _syncTimer = new(TimeSpan.FromMinutes(1));

    public ApiFaceMatchService(IFaceDataRepository faceRepo, ILogger<ApiFaceMatchService> logger)
    {
        _faceRepo = faceRepo;
        _logger = logger;
        _ = SyncWithApiAsync();
        _ = RunPeriodicSyncAsync();
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
                        _registeredFaces[record.ProfileId] = new RegisteredFace
                        {
                            ProfileId = record.ProfileId,
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
                _logger.LogInformation("[MatchService] Da dong bo {Count} nhat ky khuon mat tu API.", _registeredFaces.Count);
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
            ProfileId = null,
            FullName = "Người lạ",
            Similarity = 0f
        };

        if (embedding.Length == 0) return result;

        lock (_lock)
        {
            float maxSim = -1f;
            Guid? bestProfileId = null;
            string bestName = "";

            foreach (var face in _registeredFaces.Values)
            {
                float simFront = CosineSimilarity(embedding, face.EmbeddingFront);
                float simLeft = CosineSimilarity(embedding, face.EmbeddingLeft);
                float simRight = CosineSimilarity(embedding, face.EmbeddingRight);

                float bestSim = Math.Max(simFront, Math.Max(simLeft, simRight));

                if (bestSim > maxSim)
                {
                    maxSim = bestSim;
                    bestProfileId = face.ProfileId;
                    bestName = face.FullName;
                }
            }

            if (maxSim >= threshold)
            {
                result.IsKnown = true;
                result.ProfileId = bestProfileId;
                result.FullName = bestName;
                result.Similarity = maxSim;
            }
            else
            {
                result.Similarity = maxSim;
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

    public bool Remove(Guid profileId)
    {
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

    private async Task RunPeriodicSyncAsync()
    {
        try
        {
            while (await _syncTimer.WaitForNextTickAsync())
            {
                await SyncWithApiAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MatchService] Periodic sync stopped unexpectedly.");
        }
    }

    public async Task RegisterProfileV2Async(EnrollmentRequestV2 enrollReq)
    {
        throw new NotImplementedException();
    }
}
