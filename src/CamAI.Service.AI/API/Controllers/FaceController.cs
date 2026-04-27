using CamAI.Common.Models;
using CamAI.Service.AI.BLL.Services;
using CamAI.Service.AI.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using Microsoft.AspNetCore.Http;

namespace CamAI.Service.AI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaceController : ControllerBase
{
    private readonly IFaceDetector _detector;
    private readonly IFaceEmbedder _embedder;
    private readonly IFaceMatchService _matchService;
    private readonly IMinioStorageService _minioStorageService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly ICameraEventLogger _eventLogger;
    private readonly ILogger<FaceController> _logger;

    public FaceController(
        IFaceDetector detector,
        IFaceEmbedder embedder,
        IFaceMatchService matchService,
        IMinioStorageService minioStorageService,
        IEnrollmentService enrollmentService,
        ICameraEventLogger eventLogger,
        ILogger<FaceController> logger)
    {
        _detector = detector;
        _embedder = embedder;
        _matchService = matchService;
        _minioStorageService = minioStorageService;
        _enrollmentService = enrollmentService;
        _eventLogger = eventLogger;
        _logger = logger;
    }

    /// <summary>
    /// Bắt đầu quá trình đăng ký từ stream cho một người.
    /// POST /api/face/enroll/start
    /// </summary>
    [HttpPost("enroll/start")]
    public async Task<IActionResult> StartEnroll([FromBody] EnrollStartRequest request)
    {
        if (string.IsNullOrEmpty(request.FullName)) 
            return BadRequest(new { success = false, message = "Vui lòng nhập tên đầy đủ" });
            
        _enrollmentService.StartEnrollment(request.FullName);
        await _eventLogger.LogEventAsync("ENROLL_START", $"Bắt đầu quá trình quét mặt cho: {request.FullName}");
        return Ok(new { success = true, message = $"Bắt đầu quét mặt cho {request.FullName}. Hãy nhìn thẳng, rồi quay trái/phải từ từ trước camera." });
    }

    /// <summary>
    /// Xem trạng thái đăng ký hiện tại.
    /// GET /api/face/enroll/status
    /// </summary>
    [HttpGet("enroll/status")]
    public IActionResult GetEnrollStatus()
    {
        var req = _enrollmentService.GetCurrentRequest();
        if (req == null) return Ok(new { active = false });

        return Ok(new
        {
            active = true,
            req.FullName,
            progress = new
            {
                front = req.EmbeddingFront != null,
                left = req.EmbeddingLeft != null,
                right = req.EmbeddingRight != null
            }
        });
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
                    r.ProfileId,
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
            string datePath = DateTime.Now.ToString("yyyy/MM/dd");
            string personId = Guid.NewGuid().ToString();
            string objectName = $"register/{datePath}/{personId}_front{extension}";
            
            string minioUrl = await _minioStorageService.UploadFileAsync("faces", objectName, ms, file.ContentType);

            // Đăng ký (Gửi thông tin ObjectName sang CamAI.API)
            await _matchService.RegisterAsync(new RegisteredFace
            {
                FullName = fullName,
                EmbeddingFront = embedding, 
                MinioFront = minioUrl // Dùng làm ảnh chính diện
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
    /// Đăng ký khuôn mặt đa góc độ (Trực diện, Trái, Phải).
    /// POST /api/face/register-multi
    /// </summary>
    [HttpPost("register-multi")]
    public async Task<IActionResult> RegisterMulti(
        IFormFile frontFile,
        IFormFile leftFile,
        IFormFile rightFile,
        [FromForm] string fullName)
    {
        if (frontFile == null || leftFile == null || rightFile == null)
            return BadRequest(new { success = false, message = "Vui lòng upload đủ 3 ảnh (Chính diện, Trái, Phải)" });

        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { success = false, message = "Vui lòng nhập tên" });

        try
        {
            var embFront = await GetEmbeddingFromFile(frontFile);
            var embLeft = await GetEmbeddingFromFile(leftFile);
            var embRight = await GetEmbeddingFromFile(rightFile);

            if (embFront == null || embLeft == null || embRight == null)
                return BadRequest(new { success = false, message = "Không thể trích xuất vector từ một trong các ảnh" });

            string personId = Guid.NewGuid().ToString();
            string datePath = DateTime.Now.ToString("yyyy/MM/dd");
            string bucket = "faces";
            
            // Upload 3 ảnh lên MinIO theo cấu trúc Năm/Tháng/Ngày
            string urlFront = await _minioStorageService.UploadFileAsync(bucket, $"register/{datePath}/{personId}_front.jpg", frontFile.OpenReadStream(), frontFile.ContentType);
            string urlLeft = await _minioStorageService.UploadFileAsync(bucket, $"register/{datePath}/{personId}_left.jpg", leftFile.OpenReadStream(), leftFile.ContentType);
            string urlRight = await _minioStorageService.UploadFileAsync(bucket, $"register/{datePath}/{personId}_right.jpg", rightFile.OpenReadStream(), rightFile.ContentType);

            // Đăng ký 3 góc độ
            await _matchService.RegisterAsync(new RegisteredFace
            {
                FullName = fullName,
                EmbeddingFront = embFront,
                EmbeddingLeft = embLeft,
                EmbeddingRight = embRight,
                MinioFront = urlFront,
                MinioLeft = urlLeft,
                MinioRight = urlRight
            });

            return Ok(new
            {
                success = true,
                message = $"Đã đăng ký khuôn mặt đa góc độ cho {fullName} thành công",
                angles = new[] { "front", "left", "right" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng ký đa góc độ");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    private async Task<float[]?> GetEmbeddingFromFile(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        using var frame = Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
        if (frame.Empty()) return null;

        var detections = _detector.Detect(frame);
        if (detections.Count != 1) return null; // Chỉ chấp nhận ảnh có đúng 1 mặt

        return _embedder.GetEmbedding(frame, detections[0]);
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
                    f.ProfileId,
                    f.FullName,
                    embeddingSize = f.EmbeddingFront != null ? f.EmbeddingFront.Length : -1
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
    [HttpDelete("{profileId}")]
    public IActionResult Delete(Guid profileId)
    {
        var removed = _matchService.Remove(profileId);
        if (removed)
            return Ok(new { success = true, message = $"Đã xóa người dùng ID: {profileId}" });

        return NotFound(new { success = false, message = $"Không tìm thấy người dùng ID: {profileId}" });
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

public class EnrollStartRequest
{
    public string FullName { get; set; } = string.Empty;
}
