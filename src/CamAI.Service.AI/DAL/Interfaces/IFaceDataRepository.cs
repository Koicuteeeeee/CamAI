using CamAI.Service.AI.DAL.Models;
using CamAI.Common.Models;

namespace CamAI.Service.AI.DAL.Interfaces;

public interface IFaceDataRepository
{
    Task<List<UserFaceRecord>> GetAllFaceEmbeddingsAsync(CancellationToken ct = default);
    Task RegisterFaceAsync(RegisteredFace face, CancellationToken ct = default);
}
