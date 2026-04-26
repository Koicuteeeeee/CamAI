using CamAI.API.DAL.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using CamAI.API.DAL.Interfaces;

namespace CamAI.API.DAL.Repositories;

/// <summary>
/// Repository AccessLog: Ghi và truy xuất nhật ký truy cập qua SP.
/// </summary>
public class AccessLogRepository : IAccessLogRepository
{
    private readonly string _connectionString;

    public AccessLogRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task InsertAsync(Guid? userId, string? minioLogImage, string? deviceImpacted, string? recognitionStatus, double? confidenceScore)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "sp_AccessLog_Insert",
            new
            {
                UserId = userId,
                MinioLogImage = minioLogImage,
                DeviceImpacted = deviceImpacted,
                RecognitionStatus = recognitionStatus,
                ConfidenceScore = confidenceScore
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<AccessLogModel>> GetHistoryAsync(int page, int pageSize)
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<AccessLogModel>(
            "sp_AccessLog_GetHistory",
            new { Page = page, PageSize = pageSize },
            commandType: CommandType.StoredProcedure
        );
    }
}
