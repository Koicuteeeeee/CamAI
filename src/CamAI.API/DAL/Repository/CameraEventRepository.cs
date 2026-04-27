using CamAI.API.DAL.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

using CamAI.API.DAL.Interfaces;

namespace CamAI.API.DAL.Repositories;

public class CameraEventRepository : ICameraEventRepository
{
    private readonly string _connectionString;

    public CameraEventRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task InsertAsync(Guid? cameraId, string? cameraName, string eventType, string description, string? createdBy = null)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "sp_CameraEvent_Insert",
            new { CameraId = cameraId, CameraName = cameraName, EventType = eventType, Description = description, CreatedBy = createdBy },
            commandType: CommandType.StoredProcedure
        );
    }
}
