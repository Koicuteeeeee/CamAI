using System;

namespace CamAI.API.DAL.Models;

public class CameraModel
{
    public Guid Id { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public string StreamUrl { get; set; } = string.Empty;
    public double RecognitionThreshold { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
