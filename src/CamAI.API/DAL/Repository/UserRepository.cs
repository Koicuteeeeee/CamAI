using CamAI.API.DAL.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using CamAI.API.DAL.Interfaces;

namespace CamAI.API.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IEnumerable<UserModel>> GetAllAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserModel>(
            "SELECT * FROM Users WHERE IsActive = 1"
        );
    }

    public async Task<UserModel?> GetByUsernameAsync(string username)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserModel>(
            "SELECT * FROM Users WHERE Username = @Username",
            new { Username = username }
        );
    }

    public async Task<Guid> RegisterAsync(string username, string? email, string? fullName, string? role, Guid? keycloakId, string? createdBy = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(
            "sp_User_Register",
            new
            {
                Username = username,
                Email = email,
                FullName = fullName,
                Role = role,
                KeycloakId = keycloakId,
                CreatedBy = createdBy
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(
            "UPDATE Users SET IsActive = 0 WHERE Id = @Id",
            new { Id = id }
        );
        return affected > 0;
    }
}
