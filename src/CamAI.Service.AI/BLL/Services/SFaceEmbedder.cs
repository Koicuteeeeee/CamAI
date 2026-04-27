using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using CamAI.Common.Models;
using CamAI.Service.AI.BLL.Interfaces;

namespace CamAI.Service.AI.BLL.Services;

public class SFaceEmbedder : IFaceEmbedder
{
    private readonly InferenceSession _session;
    private readonly int _inputSize = 112;
    private readonly ILogger<SFaceEmbedder> _logger;

    // ArcFace/SFace standard reference landmarks (trên ảnh 112x112)
    private static readonly double[,] REFERENCE_LANDMARKS = new double[,]
    {
        { 38.2946, 51.6963 },  // Right eye (viewer's left)
        { 73.5318, 51.5014 },  // Left eye (viewer's right)
        { 56.0252, 71.7366 },  // Nose tip
        { 41.5493, 92.3655 },  // Right mouth corner
        { 70.7299, 92.2041 }   // Left mouth corner
    };

    public SFaceEmbedder(string modelPath, ILogger<SFaceEmbedder> logger)
    {
        _logger = logger;
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model SFace không tìm thấy: {modelPath}");

        _session = new InferenceSession(modelPath);
        _logger.LogInformation("SFace model loaded: {Path}", modelPath);
    }

    public float[] GetEmbedding(Mat faceImage)
    {
        if (faceImage == null || faceImage.Empty()) return Array.Empty<float>();
        
        _logger.LogWarning("[SFace] GetEmbedding called without landmarks - alignment skipped, accuracy may be reduced.");

        using var resized = new Mat();
        Cv2.Resize(faceImage, resized, new Size(_inputSize, _inputSize));
        var inputTensor = Preprocess(resized);
        return RunInference(inputTensor);
    }

    public float[] GetEmbedding(Mat frame, FaceDetectionResult detection)
    {
        if (frame == null || frame.Empty() || detection == null)
            return Array.Empty<float>();

        // Nếu không có Landmarks, crop & resize (fallback)
        if (detection.Landmarks == null || detection.Landmarks.Length < 5)
        {
            _logger.LogWarning("[SFace] Landmarks not available - falling back to crop+resize.");
            var rect = new Rect(
                Math.Max(0, detection.X),
                Math.Max(0, detection.Y),
                Math.Min(detection.Width, frame.Width - Math.Max(0, detection.X)),
                Math.Min(detection.Height, frame.Height - Math.Max(0, detection.Y))
            );
            if (rect.Width <= 0 || rect.Height <= 0) return Array.Empty<float>();
            using var cropped = new Mat(frame, rect);
            return GetEmbedding(cropped);
        }

        // Sử dụng Umeyama Similarity Transform (chuẩn SFace/ArcFace)
        using var aligned = AlignFaceUmeyama(frame, detection.Landmarks);
        var inputTensor = Preprocess(aligned);
        return RunInference(inputTensor);
    }

    /// <summary>
    /// Umeyama Similarity Transform: Tính ma trận biến đổi tương tự (scale + rotation + translation)
    /// từ 5 điểm landmark nguồn sang 5 điểm landmark chuẩn, giống InsightFace/ArcFace.
    /// </summary>
    private Mat AlignFaceUmeyama(Mat frame, Point2f[] landmarks)
    {
        int n = 5;

        // 1. Tính trọng tâm (centroid) của nguồn và đích
        double srcMeanX = 0, srcMeanY = 0, dstMeanX = 0, dstMeanY = 0;
        for (int i = 0; i < n; i++)
        {
            srcMeanX += landmarks[i].X;
            srcMeanY += landmarks[i].Y;
            dstMeanX += REFERENCE_LANDMARKS[i, 0];
            dstMeanY += REFERENCE_LANDMARKS[i, 1];
        }
        srcMeanX /= n; srcMeanY /= n;
        dstMeanX /= n; dstMeanY /= n;

        // 2. Tính phương sai của nguồn
        double srcVar = 0;
        for (int i = 0; i < n; i++)
        {
            double dx = landmarks[i].X - srcMeanX;
            double dy = landmarks[i].Y - srcMeanY;
            srcVar += dx * dx + dy * dy;
        }
        srcVar /= n;

        // 3. Tính Covariance Matrix (2x2)
        double cov00 = 0, cov01 = 0, cov10 = 0, cov11 = 0;
        for (int i = 0; i < n; i++)
        {
            double sx = landmarks[i].X - srcMeanX;
            double sy = landmarks[i].Y - srcMeanY;
            double dx = REFERENCE_LANDMARKS[i, 0] - dstMeanX;
            double dy = REFERENCE_LANDMARKS[i, 1] - dstMeanY;

            cov00 += dx * sx;
            cov01 += dx * sy;
            cov10 += dy * sx;
            cov11 += dy * sy;
        }
        cov00 /= n; cov01 /= n; cov10 /= n; cov11 /= n;

        // 4. SVD (2x2 matrix closed-form)
        // Ma trận cov = U * S * Vt
        using var covMat = new Mat(2, 2, MatType.CV_64FC1);
        covMat.Set(0, 0, cov00);
        covMat.Set(0, 1, cov01);
        covMat.Set(1, 0, cov10);
        covMat.Set(1, 1, cov11);

        var W = new Mat();
        var U = new Mat();
        var Vt = new Mat();
        Cv2.SVDecomp(covMat, W, U, Vt);

        // 5. Tính D (Reflection correction)
        double detU = U.At<double>(0, 0) * U.At<double>(1, 1) - U.At<double>(0, 1) * U.At<double>(1, 0);
        double detVt = Vt.At<double>(0, 0) * Vt.At<double>(1, 1) - Vt.At<double>(0, 1) * Vt.At<double>(1, 0);
        double d = (detU * detVt < 0) ? -1.0 : 1.0;

        // 6. Rotation matrix R = U * diag(1, d) * Vt
        double r00 = U.At<double>(0, 0) * Vt.At<double>(0, 0) + d * U.At<double>(0, 1) * Vt.At<double>(1, 0);
        double r01 = U.At<double>(0, 0) * Vt.At<double>(0, 1) + d * U.At<double>(0, 1) * Vt.At<double>(1, 1);
        double r10 = U.At<double>(1, 0) * Vt.At<double>(0, 0) + d * U.At<double>(1, 1) * Vt.At<double>(1, 0);
        double r11 = U.At<double>(1, 0) * Vt.At<double>(0, 1) + d * U.At<double>(1, 1) * Vt.At<double>(1, 1);

        // 7. Scale
        double traceS = W.At<double>(0) + d * W.At<double>(1);
        double scale = (srcVar > 1e-10) ? traceS / srcVar : 1.0;

        // 8. Translation
        double tx = dstMeanX - scale * (r00 * srcMeanX + r01 * srcMeanY);
        double ty = dstMeanY - scale * (r10 * srcMeanX + r11 * srcMeanY);

        // 9. Tạo Affine Matrix (2x3)
        using var affineMatrix = new Mat(2, 3, MatType.CV_64FC1);
        affineMatrix.Set(0, 0, scale * r00);
        affineMatrix.Set(0, 1, scale * r01);
        affineMatrix.Set(0, 2, tx);
        affineMatrix.Set(1, 0, scale * r10);
        affineMatrix.Set(1, 1, scale * r11);
        affineMatrix.Set(1, 2, ty);

        // 10. Warp ảnh
        var aligned = new Mat();
        Cv2.WarpAffine(frame, aligned, affineMatrix, new Size(_inputSize, _inputSize),
            InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));

        // Cleanup
        W.Dispose(); U.Dispose(); Vt.Dispose();

        return aligned;
    }

    private DenseTensor<float> Preprocess(Mat img)
    {
        var tensor = new DenseTensor<float>(new[] { 1, 3, _inputSize, _inputSize });
        var indexer = img.GetGenericIndexer<Vec3b>();

        for (int y = 0; y < _inputSize; y++)
        {
            for (int x = 0; x < _inputSize; x++)
            {
                Vec3b color = indexer[y, x];
                // SFace: [0, 255] BGR Planar - KHÔNG trừ mean, KHÔNG chia std
                tensor[0, 0, y, x] = (float)color.Item0; // B
                tensor[0, 1, y, x] = (float)color.Item1; // G
                tensor[0, 2, y, x] = (float)color.Item2; // R
            }
        }
        return tensor;
    }

    private float[] RunInference(DenseTensor<float> inputTensor)
    {
        var inputName = _session.InputNames[0];
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };

        using var output = _session.Run(inputs);
        var result = output.First().AsTensor<float>().ToArray();
        return L2Normalize(result);
    }

    private float[] L2Normalize(float[] vector)
    {
        float sumSq = 0;
        for (int i = 0; i < vector.Length; i++) sumSq += vector[i] * vector[i];
        float norm = (float)Math.Sqrt(sumSq);
        if (norm < 1e-10f) return vector;
        for (int i = 0; i < vector.Length; i++) vector[i] /= norm;
        return vector;
    }

    public void Dispose()
    {
        _session?.Dispose();
        GC.SuppressFinalize(this);
    }
}
