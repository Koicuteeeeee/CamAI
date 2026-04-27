using CamAI.API.DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamAI.API.DAL.Interfaces;

public interface IUserRepository
{
    Task<IEnumerable<UserModel>> GetAllAsync();
    Task<UserModel?> GetByUsernameAsync(string username);
    Task<Guid> RegisterAsync(string username, string? email, string? fullName, string? role, Guid? keycloakId, string? createdBy = null);
    Task<bool> DeleteAsync(Guid id);
}
