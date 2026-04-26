using CamAI.API.DAL.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

using CamAI.API.DAL.Interfaces;

namespace CamAI.API.DAL.Repositories;

public class CameraRepository : ICameraRepository
{
    private readonly string _connectionString;

    public CameraRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IEnumerable<CameraModel>> GetAllActiveAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<CameraModel>(
            "sp_Camera_GetAllActive",
            commandType: CommandType.StoredProcedure
        );
    }
}
