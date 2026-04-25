using CamAI.Common.Interfaces;
using CamAI.Common.Models;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;

namespace CamAI.Service.AI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaceController : ControllerBase
{
    private readonly IFaceDetector _detector;
    private readonly IFaceEmbedder _embedder;
    private readonly IFaceMatchService _matchService;
    private readonly IMinioStorageService _minioStorageService;
    private readonly ILogger<FaceController> _logger;

    public FaceController(
        IFaceDetector detector,
        IFaceEmbedder embedder,
        IFaceMatchService matchService,
        IMinioStorageService minioStorageService,
        ILogger<FaceController> logger)
    {
        _detector = detector;
        _embedder = embedder;
        _matchService = matchService;
        _minioStorageService = minioStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Nhận diện khuôn mặt từ ảnh upload.
    /// POST /api/face/recognize
    /// </summary>
    [HttpPost("recognize")]
    public async Task<IActionResult> Recognize(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Vui lòng upload ảnh" });

        try
        {
            // Đọc file thành byte array -> Mat
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            using var frame = Cv2.ImDecode(imageBytes, ImreadModes.Color);

            if (frame.Empty())
                return BadRequest(new { success = false, message = "Không đọc được ảnh" });

            // 1. Detect tất cả khuôn mặt
            var detections = _detector.Detect(frame);

            if (detections.Count == 0)
                return Ok(new { success = true, message = "Không phát hiện khuôn mặt nào", faces = Array.Empty<object>() });

            // 2. Với mỗi mặt: crop -> embedding -> match
            var results = new List<FaceRecognitionResult>();
            foreach (var det in detections)
            {
                var rect = new Rect(det.X, det.Y, det.Width, det.Height);
                rect = ClampRect(rect, frame.Width, frame.Height);

                _logger.LogInformation("Recognize Crop: X={X}, Y={Y}, W={W}, H={H}", rect.X, rect.Y, rect.Width, rect.Height);

                var embedding = _embedder.GetEmbedding(frame, det);
                
                float embSum = embedding.Sum();
                _logger.LogInformation("Embedding Sum: {Sum}", embSum);

                var matchResult = _matchService.Match(embedding);
                matchResult.FaceRegion = det;
                results.Add(matchResult);
            }

            return Ok(new
            {
                success = true,
                totalFaces = results.Count,
                knownFaces = results.Count(r => r.IsKnown),
                faces = results.Select(r => new
                {
                    r.IsKnown,
                    r.UserId,
                    r.FullName,
                    similarity = Math.Round(r.Similarity, 3),
                    region = new { r.FaceRegion.X, r.FaceRegion.Y, r.FaceRegion.Width, r.FaceRegion.Height }
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nhận diện");
            return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
        }
    }

    /// <summary>
    /// Đăng ký khuôn mặt mới.
    /// POST /api/face/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        IFormFile file,
        [FromForm] string fullName)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Vui lòng upload ảnh khuôn mặt" });

        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { success = false, message = "Vui lòng nhập tên" });

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            using var frame = Cv2.ImDecode(imageBytes, ImreadModes.Color);

            if (frame.Empty())
                return BadRequest(new { success = false, message = "Không đọc được ảnh" });

            // Detect mặt
            var detections = _detector.Detect(frame);
            if (detections.Count == 0)
                return BadRequest(new { success = false, message = "Không phát hiện khuôn mặt trong ảnh" });

            if (detections.Count > 1)
                return BadRequest(new { success = false, message = "Ảnh chứa nhiều hơn 1 khuôn mặt, vui lòng chụp lại" });

            // Crop và lấy embedding
            var det = detections[0];
            var rect = new Rect(det.X, det.Y, det.Width, det.Height);
            rect = ClampRect(rect, frame.Width, frame.Height);

            _logger.LogInformation("Register Crop: X={X}, Y={Y}, W={W}, H={H}", rect.X, rect.Y, rect.Width, rect.Height);

            var embedding = _embedder.GetEmbedding(frame, det);
            
            float embSum = embedding.Sum();
            _logger.LogInformation("Register Embedding Sum: {Sum}", embSum);

            // Gọi lên MinIO lưu hình ảnh gốc
            ms.Position = 0;
            string extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension)) extension = ".jpg";
            string objectName = $"faces/{Guid.NewGuid()}{extension}";
            string minioUrl = await _minioStorageService.UploadFileAsync("cam-ai-faces", objectName, ms, file.ContentType);

            // Đăng ký (Gửi thông tin ObjectName sang CamAI.API)
            _matchService.Register(new RegisteredFace
            {
                FullName = fullName,
                Embedding = embedding,
                MinioObjectName = minioUrl
            });

            return Ok(new
            {
                success = true,
                message = $"Đã gửi yêu cầu đăng ký khuôn mặt cho {fullName}",
                embeddingSize = embedding.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng ký khuôn mặt");
            return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách người đã đăng ký.
    /// GET /api/face/registered
    /// </summary>
    [HttpGet("registered")]
    public IActionResult GetRegistered()
    {
        try 
        {
            var faces = _matchService.GetAllRegistered();
            return Ok(new
            {
                success = true,
                count = faces?.Count ?? -1,
                users = faces?.Select(f => new
                {
                    f.UserId,
                    f.FullName,
                    embeddingSize = f.Embedding != null ? f.Embedding.Length : -1
                }).ToList()
            });
        }
        catch(Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }
    }

    /// <summary>
    /// Xóa người dùng khỏi hệ thống.
    /// DELETE /api/face/{userId}
    /// </summary>
    [HttpDelete("{userId}")]
    public IActionResult Delete(Guid userId)
    {
        var removed = _matchService.Remove(userId);
        if (removed)
            return Ok(new { success = true, message = $"Đã xóa người dùng ID: {userId}" });

        return NotFound(new { success = false, message = $"Không tìm thấy người dùng ID: {userId}" });
    }

    private Rect ClampRect(Rect rect, int maxWidth, int maxHeight)
    {
        int x = Math.Max(0, rect.X);
        int y = Math.Max(0, rect.Y);
        int w = Math.Min(rect.Width, maxWidth - x);
        int h = Math.Min(rect.Height, maxHeight - y);
        return new Rect(x, y, Math.Max(1, w), Math.Max(1, h));
    }
}
