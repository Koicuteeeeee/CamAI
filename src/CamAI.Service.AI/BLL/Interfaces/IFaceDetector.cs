using CamAI.Common.Models;
using OpenCvSharp;

namespace CamAI.Service.AI.BLL.Interfaces;

public interface IFaceDetector
{
    List<FaceDetectionResult> Detect(Mat image, float confidenceThreshold = 0.4f);
}
