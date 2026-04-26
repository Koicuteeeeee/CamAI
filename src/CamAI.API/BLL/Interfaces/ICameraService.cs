using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Interfaces;

public interface ICameraService
{
    Task<IEnumerable<CameraModel>> GetAllActiveAsync();
}
