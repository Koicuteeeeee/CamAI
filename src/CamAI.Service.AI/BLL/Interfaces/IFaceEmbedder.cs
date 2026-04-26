using CamAI.Common.Models;
using OpenCvSharp;

namespace CamAI.Service.AI.BLL.Interfaces;

public interface IFaceEmbedder
{
    float[] GetEmbedding(Mat image, FaceDetectionResult detection);
}
