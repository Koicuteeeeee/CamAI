namespace CamAI.API.DAL.Models;

public class CameraEventModel
{
    public Guid Id { get; set; }
    public DateTime EventTime { get; set; }
    public Guid? CameraId { get; set; }
    public string? CameraName { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
