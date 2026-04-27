using System;
using System.Collections.Generic;

namespace CamAI.Service.AI.DAL.Models;

public class UserFaceRecord
{
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public float[] EmbeddingFront { get; set; } = Array.Empty<float>();
    public float[] EmbeddingLeft { get; set; } = Array.Empty<float>();
    public float[] EmbeddingRight { get; set; } = Array.Empty<float>();
    public string MinioFront { get; set; } = string.Empty;
    public string MinioLeft { get; set; } = string.Empty;
    public string MinioRight { get; set; } = string.Empty;
}

public class ApiFaceResponse
{
    public bool Success { get; set; }
    public List<UserFaceRecord> Data { get; set; } = new();
}

/// <summary>
/// Record V2: Một dòng embedding đơn lẻ từ bảng FaceEmbeddings.
/// </summary>
public class FaceEmbeddingRecord
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AngleLabel { get; set; } = string.Empty;
    public double? AngleDegree { get; set; }
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string MinioImageUrl { get; set; } = string.Empty;
    public double CaptureQuality { get; set; }
}
