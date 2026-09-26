using System.Collections.Concurrent;
using System.Threading.Channels;
using Serilog.Core;
using Serilog.Events;

namespace FunkArr.Api;

public sealed class RingBufferSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEntry> _buffer = new();
    private readonly int _capacity;
    private readonly Channel<LogEntry> _channel = Channel.CreateBounded<LogEntry>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });

    public RingBufferSink(int capacity = 500)
    {
        _capacity = capacity;
    }

    public void Emit(LogEvent logEvent)
    {
        var entry = new LogEntry(
            logEvent.Timestamp,
            logEvent.Level.ToString(),
            logEvent.RenderMessage(),
            logEvent.Properties.GetValueOrDefault("SourceContext")?.ToString()?.Trim('"'),
            logEvent.Exception?.Message);

        _buffer.Enqueue(entry);
        while (_buffer.Count > _capacity)
        {
            _buffer.TryDequeue(out _);
        }

        _channel.Writer.TryWrite(entry);
    }

    public LogEntry[] GetEntries() => [.. _buffer];

    public ChannelReader<LogEntry> Reader => _channel.Reader;
}

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? SourceContext,
    string? Exception);
