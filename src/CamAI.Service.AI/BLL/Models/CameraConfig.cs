using System;

namespace CamAI.Service.AI.BLL.Models;

public class CameraConfig
{
    public Guid Id { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public string StreamUrl { get; set; } = string.Empty;
    public double RecognitionThreshold { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}
