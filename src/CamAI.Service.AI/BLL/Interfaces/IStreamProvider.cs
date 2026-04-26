namespace CamAI.Service.AI.BLL.Interfaces;

public interface IStreamProvider
{
    void SetLastFrame(byte[] frameBytes);
    byte[] GetLastFrame();
}
