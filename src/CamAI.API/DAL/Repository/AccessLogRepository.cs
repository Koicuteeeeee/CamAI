using CamAI.API.DAL.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CamAI.API.DAL.Repository;

/// <summary>
/// Repository AccessLog: Ghi và truy xuất nhật ký truy cập qua SP.
/// </summary>
public class AccessLogRepository
{
    private readonly string _connectionString;

    public AccessLogRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task InsertAsync(Guid? userId, string actionTaken, string? minioLogImage, string? deviceImpacted)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "sp_AccessLog_Insert",
            new
            {
                UserId = userId,
                ActionTaken = actionTaken,
                MinioLogImage = minioLogImage,
                DeviceImpacted = deviceImpacted
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
