namespace FunkArr.Download.Tests;

public sealed class SrtFormatTests
{
    private readonly SrtFormat _format = new();

    [Fact]
    public void CanParse_detects_srt()
    {
        Assert.True(_format.CanParse("1\n00:00:01,000 --> 00:00:02,000\nHi\n"));
    }

    [Fact]
    public void CanParse_rejects_vtt()
    {
        Assert.False(_format.CanParse("WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHi"));
    }

    [Fact]
    public void Parse_basic_entries()
    {
        var srt = "1\n00:00:01,000 --> 00:00:03,000\nHallo Welt\n\n2\n00:00:05,000 --> 00:00:07,000\nZweite Zeile\n";

        var cues = _format.Parse(srt);

        Assert.Equal(2, cues.Count);
        Assert.Equal("Hallo Welt", cues[0].Text);
        Assert.Equal(TimeSpan.FromSeconds(1), cues[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(3), cues[0].End);
    }

    [Fact]
    public void Parse_dot_separator()
    {
        var srt = "1\n00:00:01.500 --> 00:00:03.500\nDot separator\n";

        var cues = _format.Parse(srt);

        var cue = Assert.Single(cues);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), cue.Start);
    }

    [Fact]
    public void Parse_multi_line_text()
    {
        var srt = "1\n00:00:01,000 --> 00:00:03,000\nLine one\nLine two\n";

        var cues = _format.Parse(srt);

        var cue = Assert.Single(cues);
        Assert.Equal("Line one\nLine two", cue.Text);
    }

    [Fact]
    public void Parse_skips_blank_entries()
    {
        var srt = "1\n00:00:01,000 --> 00:00:03,000\n\n\n2\n00:00:05,000 --> 00:00:07,000\nReal text\n";

        var cues = _format.Parse(srt);

        var cue = Assert.Single(cues);
        Assert.Equal("Real text", cue.Text);
    }
}
