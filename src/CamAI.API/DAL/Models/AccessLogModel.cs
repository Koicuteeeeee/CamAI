namespace CamAI.API.DAL.Models;

public class AccessLogModel
{
    public Guid Id { get; set; }
    public DateTime LogTime { get; set; }
    public Guid? UserId { get; set; }
    public string? RecognitionStatus { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? MinioLogImage { get; set; }
    public string? DeviceImpacted { get; set; }
}
