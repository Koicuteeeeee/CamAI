using CamAI.API.DAL.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using CamAI.API.DAL.Interfaces;

namespace CamAI.API.DAL.Repositories;

public class AccessLogRepository : IAccessLogRepository
{
    private readonly string _connectionString;

    public AccessLogRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task InsertAsync(Guid? profileId, string? fullName, string? minioLogImage, string? deviceImpacted, string? recognitionStatus, double? confidenceScore, string? createdBy = null)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "sp_AccessLog_Insert",
            new
            {
                ProfileId = profileId,
                FullName = fullName,
                MinioLogImage = minioLogImage,
                DeviceImpacted = deviceImpacted,
                RecognitionStatus = recognitionStatus,
                Similarity = confidenceScore,
                CreatedBy = createdBy
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page, int pageSize)
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<AccessLogModel>(
            "SELECT * FROM AccessLogs ORDER BY LogTime DESC OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY",
            new { Page = page, PageSize = pageSize }
        );
    }
}
