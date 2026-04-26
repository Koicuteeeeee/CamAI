using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Interfaces;
using CamAI.API.DAL.Models;

namespace CamAI.API.BLL.Services;

public class CameraService : ICameraService
{
    private readonly ICameraRepository _cameraRepository;

    public CameraService(ICameraRepository cameraRepository)
    {
        _cameraRepository = cameraRepository;
    }

    public async Task<IEnumerable<CameraModel>> GetAllActiveAsync()
    {
        return await _cameraRepository.GetAllActiveAsync();
    }
}
