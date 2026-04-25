using CamAI.API.DAL.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CamAI.API.DAL.Repository;

/// <summary>
/// Repository User: Mọi thao tác DB đều qua Stored Procedure.
/// </summary>
public class UserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IEnumerable<UserModel>> GetAllActiveAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserModel>(
            "sp_User_GetAllActive",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<UserModel?> GetByIdAsync(Guid id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserModel>(
            "sp_User_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Guid> RegisterAsync(string username, string fullName, byte[] faceEmbedding, string minioObjectName)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(
            "sp_User_Register",
            new
            {
                Username = username,
                FullName = fullName,
                FaceEmbedding = faceEmbedding,
                MinioObjectName = minioObjectName
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<UserFaceModel>> GetAllFaceEmbeddingsAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserFaceModel>(
            "sp_UserFace_GetAll",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<bool> DeleteAsync(Guid userId)
    {
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(
            "sp_User_Delete",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure
        );
        return affected > 0;
    }
}
