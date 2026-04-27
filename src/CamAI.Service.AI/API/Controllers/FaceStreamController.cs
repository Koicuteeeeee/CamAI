using Microsoft.AspNetCore.Mvc;
using CamAI.Service.AI.BLL.Interfaces;
using System.Net.Http.Headers;

namespace CamAI.Service.AI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaceStreamController : ControllerBase
{
    private readonly IStreamProvider _streamProvider;

    public FaceStreamController(IStreamProvider streamProvider)
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
        await ProcessStream(ct, () => _streamProvider.GetLastFrame());
    }

    /// <summary>
    /// Phát luồng MJPEG KHÔNG CÓ khung nhận diện (cho Web CMS chính).
    /// Truy cập: http://localhost:5120/api/facestream/live-clean
    /// </summary>
    [HttpGet("live-clean")]
    public async Task GetLiveStreamClean(CancellationToken ct)
    {
        await ProcessStream(ct, () => _streamProvider.GetLastRawFrame());
    }

    private async Task ProcessStream(CancellationToken ct, Func<byte[]> frameGetter)
    {
        var response = Response;
        response.ContentType = "multipart/x-mixed-replace; boundary=--frame";
        response.StatusCode = 200;

        while (!ct.IsCancellationRequested)
        {
            var frame = frameGetter();
            if (frame != null && frame.Length > 0)
            {
                await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("--frame\r\n"), ct);
                await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("Content-Type: image/jpeg\r\n"), ct);
                await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"Content-Length: {frame.Length}\r\n\r\n"), ct);
                await response.Body.WriteAsync(frame, ct);
                await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("\r\n"), ct);
                await response.Body.FlushAsync(ct);
            }

            await Task.Delay(33, ct);
        }
    }
}
