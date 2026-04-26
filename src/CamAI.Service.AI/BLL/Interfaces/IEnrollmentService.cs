using CamAI.Service.AI.DAL.Models;

namespace CamAI.Service.AI.BLL.Interfaces;

public interface IEnrollmentService
{
    void StartEnrollment(string fullName);
    EnrollmentRequest? GetCurrentRequest();
    void ClearRequest();
    bool UpdateEmbedding(float[] embedding, string angle, byte[] image);
}
