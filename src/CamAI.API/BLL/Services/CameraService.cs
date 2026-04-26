using CamAI.API.BLL.Interfaces;
using CamAI.API.DAL.Models;
using CamAI.API.DAL.Repository;

namespace CamAI.API.BLL.Services;

public class CameraService : ICameraService
{
    private readonly CameraRepository _cameraRepository;

    public CameraService(CameraRepository cameraRepository)
    {
        _cameraRepository = cameraRepository;
    }

    public async Task<IEnumerable<CameraModel>> GetAllActiveAsync()
    {
        return await _cameraRepository.GetAllActiveAsync();
    }
}
