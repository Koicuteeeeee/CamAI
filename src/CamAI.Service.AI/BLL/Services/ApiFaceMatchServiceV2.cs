using CamAI.Common.Models;
using CamAI.Service.AI.DAL.Interfaces;
using CamAI.Service.AI.DAL.Models;
using CamAI.Service.AI.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace CamAI.Service.AI.BLL.Services;

/// <summary>
/// So khớp khuôn mặt V2 (Đa góc độ): Hỗ trợ N embedding mỗi người.
/// Tương thích ngược với bảng UserFaces cũ và bảng FaceEmbeddings mới.
/// </summary>
public class ApiFaceMatchServiceV2 : IFaceMatchService
{
    private readonly IFaceDataRepository _faceRepo;
    private readonly ILogger<ApiFaceMatchServiceV2> _logger;
    private Dictionary<Guid, RegisteredFaceV2> _registeredFaces = new();
    private readonly object _lock = new();
    private readonly PeriodicTimer _syncTimer = new(TimeSpan.FromMinutes(1));

    public ApiFaceMatchServiceV2(IFaceDataRepository faceRepo, ILogger<ApiFaceMatchServiceV2> logger)
    {
        _faceRepo = faceRepo;
        _logger = logger;
        _ = SyncWithApiAsync();
        _ = RunPeriodicSyncAsync();
    }

    /// <summary>
    /// Đồng bộ dữ liệu từ API — hỗ trợ cả V1 (3 cột) lẫn V2 (N dòng).
    /// </summary>
    public async Task SyncWithApiAsync()
    {
        try
        {
            // Ưu tiên đọc từ bảng FaceEmbeddings mới (V2)
            var v2Data = await _faceRepo.GetAllFaceEmbeddingsV2Async();
            if (v2Data != null && v2Data.Any())
            {
                lock (_lock)
                {
                    _registeredFaces.Clear();
                    // Group theo ProfileId
                    foreach (var group in v2Data.GroupBy(e => e.ProfileId))
                    {
                        _registeredFaces[group.Key] = new RegisteredFaceV2
                        {
                            ProfileId = group.Key,
                            FullName = group.First().FullName,
                            Embeddings = group.Select(e => new FaceAngleEmbedding
                            {
                                Id = e.Id,
                                AngleLabel = e.AngleLabel,
                                AngleDegree = e.AngleDegree,
                                Embedding = e.Embedding,
                                MinioImageUrl = e.MinioImageUrl,
                                CaptureQuality = e.CaptureQuality
                            }).ToList()
                        };
                    }
                }
                _logger.LogInformation("[MatchServiceV2] Da dong bo {Count} profiles ({Total} embeddings) tu API (V2).",
                    _registeredFaces.Count, v2Data.Count());
                return;
            }

            // Fallback: Đọc từ bảng UserFaces cũ (V1 - 3 góc cố định)
            var v1Data = await _faceRepo.GetAllFaceEmbeddingsAsync();
            if (v1Data != null)
            {
                lock (_lock)
                {
                    _registeredFaces.Clear();
                    foreach (var record in v1Data)
                    {
                        var embeddings = new List<FaceAngleEmbedding>();
                        if (record.EmbeddingFront?.Length > 0)
                            embeddings.Add(new FaceAngleEmbedding { AngleLabel = "front", Embedding = record.EmbeddingFront, MinioImageUrl = record.MinioFront });
                        if (record.EmbeddingLeft?.Length > 0)
                            embeddings.Add(new FaceAngleEmbedding { AngleLabel = "left", Embedding = record.EmbeddingLeft, MinioImageUrl = record.MinioLeft });
                        if (record.EmbeddingRight?.Length > 0)
                            embeddings.Add(new FaceAngleEmbedding { AngleLabel = "right", Embedding = record.EmbeddingRight, MinioImageUrl = record.MinioRight });

                        _registeredFaces[record.ProfileId] = new RegisteredFaceV2
                        {
                            ProfileId = record.ProfileId,
                            FullName = record.FullName,
                            Embeddings = embeddings
                        };
                    }
                }
                _logger.LogInformation("[MatchServiceV2] Fallback V1: Da dong bo {Count} profiles tu API.", _registeredFaces.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi dong bo khuon mat tu API.");
        }
    }

    /// <summary>
    /// So khớp 1 embedding với TẤT CẢ embedding đã đăng ký (N góc mỗi người).
    /// Lấy MAX similarity trên toàn bộ góc của mỗi người.
    /// </summary>
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
                // Duyệt tất cả N embedding của người này
                float bestSimForPerson = -1f;
                foreach (var angle in face.Embeddings)
                {
                    if (angle.Embedding == null || angle.Embedding.Length == 0) continue;
                    float sim = CosineSimilarity(embedding, angle.Embedding);
                    if (sim > bestSimForPerson)
                        bestSimForPerson = sim;
                }

                if (bestSimForPerson > maxSim)
                {
                    maxSim = bestSimForPerson;
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
        // Backward compatibility: Chuyển V1 thành V2
        _logger.LogWarning("[MatchServiceV2] RegisterAsync(V1) called — converting to V2 format.");
        await SyncWithApiAsync();
    }

    public async Task RegisterProfileV2Async(EnrollmentRequestV2 enrollReq)
    {
        try
        {
            // 1. Tạo profile mới
            Guid newProfileId = await _faceRepo.RegisterProfileV2Async(enrollReq.FullName);

            // 2. Add embeddings
            foreach (var angle in enrollReq.CapturedAngles)
            {
                await _faceRepo.AddEmbeddingAsync(
                    newProfileId,
                    angle.AngleLabel,
                    angle.AngleDegree,
                    angle.Embedding,
                    angle.MinioImageUrl,
                    angle.Quality
                );
            }
            await SyncWithApiAsync();
            _logger.LogInformation("✅ [MatchServiceV2] Đã đăng ký XONG đa góc độ cho: {Name}", enrollReq.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MatchServiceV2] Lỗi khi đăng ký khuôn mặt liên tục.");
        }
    }

    public List<RegisteredFace> GetAllRegistered()
    {
        // Backward compatibility
        lock (_lock)
        {
            return _registeredFaces.Values.Select(f => new RegisteredFace
            {
                ProfileId = f.ProfileId,
                FullName = f.FullName,
                EmbeddingFront = f.Embeddings.FirstOrDefault(e => e.AngleLabel == "front")?.Embedding ?? [],
                EmbeddingLeft = f.Embeddings.FirstOrDefault(e => e.AngleLabel == "left")?.Embedding ?? [],
                EmbeddingRight = f.Embeddings.FirstOrDefault(e => e.AngleLabel == "right")?.Embedding ?? [],
            }).ToList();
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
            _logger.LogWarning(ex, "[MatchServiceV2] Periodic sync stopped unexpectedly.");
        }
    }
}
