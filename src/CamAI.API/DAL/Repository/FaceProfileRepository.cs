using CamAI.API.DAL.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using CamAI.API.DAL.Interfaces;

namespace CamAI.API.DAL.Repositories;

public class FaceProfileRepository : IFaceProfileRepository
{
    private readonly string _connectionString;

    public FaceProfileRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IEnumerable<FaceProfileModel>> GetAllAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<FaceProfileModel>(
            "sp_FaceProfile_GetAll",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<bool> DeleteAsync(Guid profileId)
    {
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(
            "sp_FaceProfile_Delete",
            new { ProfileId = profileId },
            commandType: CommandType.StoredProcedure
        );
        return affected > 0;
    }

    public async Task<IEnumerable<FaceEmbeddingModel>> GetAllFaceEmbeddingsV2Async()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<FaceEmbeddingModel>(
            "sp_FaceEmbedding_GetAll",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Guid> RegisterProfileV2Async(string fullName, string? externalCode = null, string? profileType = "Resident", string? createdBy = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(
            "sp_FaceProfile_RegisterV2",
            new
            {
                FullName = fullName,
                ExternalCode = externalCode,
                ProfileType = profileType,
                CreatedBy = createdBy
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Guid> AddEmbeddingAsync(Guid profileId, string angleLabel, float? angleDegree, byte[] embedding, string? minioImageUrl, float? captureQuality, string? createdBy = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(
            "sp_FaceEmbedding_Add",
            new
            {
                ProfileId = profileId,
                AngleLabel = angleLabel,
                AngleDegree = angleDegree,
                Embedding = embedding,
                MinioImageUrl = minioImageUrl,
                CaptureQuality = captureQuality,
                CreatedBy = createdBy
            },
            commandType: CommandType.StoredProcedure
        );
    }
}
