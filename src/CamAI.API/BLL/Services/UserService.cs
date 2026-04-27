using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Interfaces;
using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<UserModel>> GetAllAsync()
        => await _userRepo.GetAllAsync();

    public async Task<UserModel?> GetByUsernameAsync(string username)
        => await _userRepo.GetByUsernameAsync(username);

    public async Task<Guid> RegisterAsync(string username, string? email, string? fullName, string? role, Guid? keycloakId, string? createdBy = null)
        => await _userRepo.RegisterAsync(username, email, fullName, role, keycloakId, createdBy);

    public async Task<bool> DeleteAsync(Guid id)
        => await _userRepo.DeleteAsync(id);
}
