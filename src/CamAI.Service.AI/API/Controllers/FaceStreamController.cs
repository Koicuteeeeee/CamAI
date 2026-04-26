using Microsoft.AspNetCore.Mvc;
using CamAI.Service.AI.BLL.Services;
using System.Net.Http.Headers;

namespace CamAI.Service.AI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaceStreamController : ControllerBase
{
    private readonly StreamProvider _streamProvider;

    public FaceStreamController(StreamProvider streamProvider)
    {
        _streamProvider = streamProvider;
    }

    /// <summary>
    /// Phát luồng MJPEG trực tiếp của Camera có vẽ khung nhận diện.
    /// Truy cập: http://localhost:5120/api/facestream/live
    /// </summary>
    [HttpGet("live")]
    public async Task GetLiveStream(CancellationToken ct)
    {
        var response = Response;
        response.ContentType = "multipart/x-mixed-replace; boundary=--frame";
        response.StatusCode = 200;

        while (!ct.IsCancellationRequested)
        {
            var frame = _streamProvider.GetLastFrame();
            if (frame != null && frame.Length > 0)
            {
                // console.WriteLine("Sending frame..."); // Test nhanh
                await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("--frame\r\n"), ct);
                await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("Content-Type: image/jpeg\r\n"), ct);
                await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"Content-Length: {frame.Length}\r\n\r\n"), ct);
                await response.Body.WriteAsync(frame, ct);
                await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("\r\n"), ct);
                await response.Body.FlushAsync(ct);
            }

            // Đồng bộ tốc độ phát khoảng 25-30fps
            await Task.Delay(40, ct);
        }
    }
}
