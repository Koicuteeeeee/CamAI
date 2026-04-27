namespace CamAI.API.DAL.Models;

public class AccessLogModel
{
    public Guid Id { get; set; }
    public DateTime LogTime { get; set; }
    public Guid? ProfileId { get; set; }
    public string? FullName { get; set; }
    public string? RecognitionStatus { get; set; }
    public double? Similarity { get; set; }
    public string? MinioLogImage { get; set; }
    public string? DeviceImpacted { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
