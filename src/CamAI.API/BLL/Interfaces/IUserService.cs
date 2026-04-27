using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserModel>> GetAllAsync();
    Task<UserModel?> GetByUsernameAsync(string username);
    Task<Guid> RegisterAsync(string username, string? email, string? fullName, string? role, Guid? keycloakId, string? createdBy = null);
    Task<bool> DeleteAsync(Guid id);
}
