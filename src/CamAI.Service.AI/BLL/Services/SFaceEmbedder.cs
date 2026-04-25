using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using CamAI.Common.Interfaces;
using CamAI.Common.Models;

namespace CamAI.Service.AI.Services;

public class SFaceEmbedder : IFaceEmbedder
{
    private readonly InferenceSession _session;
    private readonly int _inputSize = 112;
    private readonly ILogger<SFaceEmbedder> _logger;

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
        using var resized = new Mat();
        Cv2.Resize(faceImage, resized, new Size(_inputSize, _inputSize));
        var inputTensor = Preprocess(resized);
        return RunInference(inputTensor);
    }

    public float[] GetEmbedding(Mat frame, FaceDetectionResult detection)
    {
        if (frame == null || frame.Empty() || detection == null || detection.Landmarks == null || detection.Landmarks.Length < 5)
        {
            if (frame != null && detection != null)
                return GetEmbedding(new Mat(frame, new Rect(detection.X, detection.Y, detection.Width, detection.Height)));
            return Array.Empty<float>();
        }

        using var aligned = AlignFace(frame, detection.Landmarks);
        var inputTensor = Preprocess(aligned);
        return RunInference(inputTensor);
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
                // SFace: [0, 255] BGR Planar
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

    private Mat AlignFace(Mat frame, Point2f[] landmarks)
    {
        var dstPoints = new Point2f[]
        {
            new Point2f(38.2946f, 51.6963f),
            new Point2f(73.5318f, 51.5014f),
            new Point2f(56.0252f, 71.7366f),
            new Point2f(41.5493f, 92.3655f),
            new Point2f(70.7299f, 92.2041f)
        };

        using var src = InputArray.Create(landmarks);
        using var dst = InputArray.Create(dstPoints);
        using var matrix = Cv2.EstimateAffinePartial2D(src, dst);
        
        var aligned = new Mat();
        if (matrix.Empty()) {
            Cv2.Resize(new Mat(frame, new Rect(0,0,Math.Min(10, frame.Width), Math.Min(10, frame.Height))), aligned, new Size(_inputSize, _inputSize));
        } else {
            Cv2.WarpAffine(frame, aligned, matrix, new Size(_inputSize, _inputSize));
        }
        return aligned;
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
