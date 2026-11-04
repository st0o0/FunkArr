using Serilog.Events;
using Serilog.Parsing;

namespace FunkArr.Api.Tests;

public sealed class RingBufferSinkTests
{
    private static readonly TimeSpan _readTimeout = TimeSpan.FromSeconds(5);
    private static readonly MessageTemplateParser _parser = new();

    private static LogEvent Event(string message) =>
        new(DateTimeOffset.UnixEpoch, LogEventLevel.Information, null, _parser.Parse(message), []);

    private static async Task<List<string>> ReadAsync(IAsyncEnumerable<LogEntry> stream, int count)
    {
        using var cts = new CancellationTokenSource(_readTimeout);
        var messages = new List<string>();
        await foreach (var entry in stream.WithCancellation(cts.Token))
        {
            messages.Add(entry.Message);
            if (messages.Count == count)
            {
                break;
            }
        }

        return messages;
    }

    [Fact]
    public async Task Every_subscriber_receives_every_entry_in_order()
    {
        var sink = new RingBufferSink();
        using var cts = new CancellationTokenSource();
        var first = ReadAsync(sink.Subscribe(cts.Token), 10);
        var second = ReadAsync(sink.Subscribe(cts.Token), 10);

        for (var i = 0; i < 10; i++)
        {
            sink.Emit(Event($"entry {i}"));
        }

        var expected = Enumerable.Range(0, 10).Select(i => $"entry {i}").ToList();
        var received = await Task.WhenAll(first, second);
        Assert.Multiple(
            () => Assert.Equal(expected, received[0]),
            () => Assert.Equal(expected, received[1]));
    }

    [Fact]
    public async Task Entries_emitted_without_subscribers_are_not_streamed_later()
    {
        var sink = new RingBufferSink();
        for (var i = 0; i < 50; i++)
        {
            sink.Emit(Event($"old {i}"));
        }

        using var cts = new CancellationTokenSource();
        var stream = ReadAsync(sink.Subscribe(cts.Token), 1);
        sink.Emit(Event("new"));

        Assert.Equal(["new"], await stream);
    }

    [Fact]
    public void Snapshot_keeps_only_the_newest_entries_up_to_capacity()
    {
        var sink = new RingBufferSink();

        for (var i = 0; i < 600; i++)
        {
            sink.Emit(Event($"entry {i}"));
        }

        var entries = sink.GetEntries();
        Assert.Equal(500, entries.Length);
        Assert.Multiple(
            () => Assert.Equal("entry 100", entries[0].Message),
            () => Assert.Equal("entry 599", entries[^1].Message));
    }

    [Fact]
    public async Task Full_subscriber_drops_its_oldest_entries_without_blocking_emit()
    {
        var sink = new RingBufferSink();
        using var cts = new CancellationTokenSource();
        _ = sink.Subscribe(cts.Token);
        var late = sink.Subscribe(cts.Token);

        for (var i = 0; i < 300; i++)
        {
            sink.Emit(Event($"entry {i}"));
        }

        var received = await ReadAsync(late, 256);
        Assert.Multiple(
            () => Assert.Equal(256, received.Count),
            () => Assert.Equal("entry 44", received[0]),
            () => Assert.Equal("entry 299", received[^1]));
    }

    [Fact]
    public async Task Cancelled_subscriber_is_removed()
    {
        var sink = new RingBufferSink();
        using var cts = new CancellationTokenSource();
        _ = sink.Subscribe(cts.Token);
        Assert.Equal(1, sink.SubscriberCount);

        await cts.CancelAsync();

        Assert.Equal(0, sink.SubscriberCount);
    }
}
