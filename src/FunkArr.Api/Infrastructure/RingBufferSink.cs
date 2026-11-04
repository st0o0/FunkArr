using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Serilog.Core;
using Serilog.Events;

namespace FunkArr.Api.Infrastructure;

public sealed class RingBufferSink(int capacity = 500) : ILogEventSink
{
    private const int _subscriberCapacity = 256;

    private readonly ConcurrentQueue<LogEntry> _buffer = new();
    private readonly ConcurrentDictionary<Guid, Channel<LogEntry>> _subscribers = new();

    public int SubscriberCount => _subscribers.Count;

    public void Emit(LogEvent logEvent)
    {
        var entry = new LogEntry(
            logEvent.Timestamp,
            logEvent.Level.ToString(),
            logEvent.RenderMessage(),
            logEvent.Properties.GetValueOrDefault("SourceContext")?.ToString().Trim('"'),
            logEvent.Exception?.Message);

        _buffer.Enqueue(entry);
        while (_buffer.Count > capacity)
        {
            _buffer.TryDequeue(out _);
        }

        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(entry);
        }
    }

    public LogEntry[] GetEntries() => [.. _buffer];

    public IAsyncEnumerable<LogEntry> Subscribe(CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(_subscriberCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        _subscribers[id] = channel;
        var registration = ct.Register(() => Unsubscribe(id));

        return ReadAsync(id, channel, registration, ct);
    }

    private async IAsyncEnumerable<LogEntry> ReadAsync(
        Guid id, Channel<LogEntry> channel, CancellationTokenRegistration registration,
        [EnumeratorCancellation] CancellationToken ct)
    {
        try
        {
            await foreach (var entry in channel.Reader.ReadAllAsync(ct))
            {
                yield return entry;
            }
        }
        finally
        {
            await registration.DisposeAsync();
            Unsubscribe(id);
        }
    }

    private void Unsubscribe(Guid id)
    {
        if (_subscribers.TryRemove(id, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }
}

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? SourceContext,
    string? Exception);
