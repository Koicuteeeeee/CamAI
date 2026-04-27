using System.Threading.Channels;

namespace CamAI.API.BLL.Services;

public record AccessLogEvent(
    Guid? ProfileId,
    string? FullName,
    string? RecognitionStatus,
    double? ConfidenceScore,
    DateTime CreatedAtUtc
);

public class AccessLogEventBus
{
    private readonly object _lock = new();
    private readonly List<Channel<AccessLogEvent>> _subscribers = new();

    public Channel<AccessLogEvent> Subscribe()
    {
        var channel = Channel.CreateUnbounded<AccessLogEvent>();
        lock (_lock)
        {
            _subscribers.Add(channel);
        }

        return channel;
    }

    public void Unsubscribe(Channel<AccessLogEvent> channel)
    {
        lock (_lock)
        {
            _subscribers.Remove(channel);
        }

        channel.Writer.TryComplete();
    }

    public void Publish(AccessLogEvent evt)
    {
        List<Channel<AccessLogEvent>> subscribers;
        lock (_lock)
        {
            subscribers = _subscribers.ToList();
        }

        foreach (var channel in subscribers)
        {
            channel.Writer.TryWrite(evt);
        }
    }
}
