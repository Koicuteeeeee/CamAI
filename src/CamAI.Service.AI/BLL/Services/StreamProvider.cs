using CamAI.Service.AI.BLL.Interfaces;

namespace CamAI.Service.AI.BLL.Services;

/// <summary>
/// Dịch vụ trung gian để lưu trữ khung hình mới nhất đã được vẽ khung nhận diện.
/// </summary>
public class StreamProvider : IStreamProvider
{
    private byte[] _lastFrame = Array.Empty<byte>();
    private byte[] _lastFrameRaw = Array.Empty<byte>();

    public void SetLastFrame(byte[] frameBytes)
    {
        _lastFrame = frameBytes;
    }

    public byte[] GetLastFrame()
    {
        return _lastFrame;
    }

    public void SetLastRawFrame(byte[] frameBytes)
    {
        _lastFrameRaw = frameBytes;
    }

    public byte[] GetLastRawFrame()
    {
        return _lastFrameRaw;
    }
}
