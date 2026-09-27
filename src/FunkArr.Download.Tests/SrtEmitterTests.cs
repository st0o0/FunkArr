namespace FunkArr.Download.Tests;

public sealed class SrtEmitterTests
{
    [Fact]
    public void Emit_standard_output()
    {
        var cues = new List<SubtitleCue>
        {
            new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), "First"),
            new(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), "Second"),
        };

        var srt = SrtEmitter.Emit(cues);

        Assert.Contains("1\n00:00:01,000 --> 00:00:03,000\nFirst", srt);
        Assert.Contains("2\n00:00:05,000 --> 00:00:07,000\nSecond", srt);
    }

    [Fact]
    public void Emit_sequential_numbering()
    {
        var cues = new List<SubtitleCue>
        {
            new(TimeSpan.Zero, TimeSpan.FromSeconds(1), "A"),
            new(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), "B"),
            new(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(5), "C"),
        };

        var srt = SrtEmitter.Emit(cues);

        Assert.StartsWith("1\n", srt);
        Assert.Contains("2\n", srt);
        Assert.Contains("3\n", srt);
    }

    [Fact]
    public void Emit_millisecond_precision()
    {
        var cues = new List<SubtitleCue>
        {
            new(new TimeSpan(0, 1, 23, 45, 678), new TimeSpan(0, 1, 23, 47, 123), "Precise"),
        };

        var srt = SrtEmitter.Emit(cues);

        Assert.Contains("01:23:45,678 --> 01:23:47,123", srt);
    }
}
