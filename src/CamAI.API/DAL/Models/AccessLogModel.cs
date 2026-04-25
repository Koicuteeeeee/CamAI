namespace CamAI.API.DAL.Models;

public class AccessLogModel
{
    public long Id { get; set; }
    public DateTime LogTime { get; set; }
    public Guid? UserId { get; set; }
    public string? FullName { get; set; }
    public string ActionTaken { get; set; } = string.Empty;
    public string? MinioLogImage { get; set; }
    public string? DeviceImpacted { get; set; }
}
