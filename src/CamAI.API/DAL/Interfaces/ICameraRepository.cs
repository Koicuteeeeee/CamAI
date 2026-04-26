using CamAI.API.DAL.Models;

namespace CamAI.API.DAL.Interfaces;

public interface ICameraRepository
{
    Task<IEnumerable<CameraModel>> GetAllActiveAsync();
}
