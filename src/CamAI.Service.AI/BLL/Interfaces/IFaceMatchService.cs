using CamAI.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamAI.Service.AI.BLL.Interfaces;

public interface IFaceMatchService
{
    Task SyncWithApiAsync();
    FaceRecognitionResult Match(float[] embedding, float threshold = 0.35f);
    Task RegisterAsync(RegisteredFace face);
    List<RegisteredFace> GetAllRegistered();
    bool Remove(Guid profileId);
}
