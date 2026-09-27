namespace FunkArr.Download.Tests;

public sealed class WebVttFormatTests
{
    private readonly WebVttFormat _format = new();

    [Fact]
    public void CanParse_detects_webvtt()
    {
        Assert.True(_format.CanParse("WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHi"));
    }

    [Fact]
    public void CanParse_rejects_srt()
    {
        Assert.False(_format.CanParse("1\n00:00:01,000 --> 00:00:02,000\nHi"));
    }

    [Fact]
    public void Parse_basic_cues()
    {
        var vtt = "WEBVTT\n\n00:00:01.000 --> 00:00:03.000\nHallo Welt\n\n00:00:05.000 --> 00:00:07.000\nZweite Zeile\n";

        var cues = _format.Parse(vtt);

        Assert.Equal(2, cues.Count);
        Assert.Equal("Hallo Welt", cues[0].Text);
        Assert.Equal(TimeSpan.FromSeconds(1), cues[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(3), cues[0].End);
        Assert.Equal("Zweite Zeile", cues[1].Text);
    }

    [Fact]
    public void Parse_short_timestamps()
    {
        var vtt = "WEBVTT\n\n01:30.000 --> 02:00.000\nShort format\n";

        var cues = _format.Parse(vtt);

        var cue = Assert.Single(cues);
        Assert.Equal(TimeSpan.FromSeconds(90), cue.Start);
        Assert.Equal(TimeSpan.FromSeconds(120), cue.End);
    }

    [Fact]
    public void Parse_skips_note_blocks()
    {
        var vtt = "WEBVTT\n\nNOTE\nThis is a comment\n\n00:00:01.000 --> 00:00:02.000\nReal text\n";

        var cues = _format.Parse(vtt);

        var cue = Assert.Single(cues);
        Assert.Equal("Real text", cue.Text);
    }

    [Fact]
    public void Parse_skips_style_blocks()
    {
        var vtt = "WEBVTT\n\nSTYLE\n::cue { color: white; }\n\n00:00:01.000 --> 00:00:02.000\nStyled\n";

        var cues = _format.Parse(vtt);

        var cue = Assert.Single(cues);
        Assert.Equal("Styled", cue.Text);
    }

    [Fact]
    public void Parse_strips_html_tags()
    {
        var vtt = "WEBVTT\n\n00:00:01.000 --> 00:00:02.000\n<b>Bold</b> and <i>italic</i>\n";

        var cues = _format.Parse(vtt);

        var cue = Assert.Single(cues);
        Assert.Equal("Bold and italic", cue.Text);
    }

    [Fact]
    public void Parse_ignores_cue_settings()
    {
        var vtt = "WEBVTT\n\n00:00:01.000 --> 00:00:02.000 position:10% align:start\nPositioned\n";

        var cues = _format.Parse(vtt);

        var cue = Assert.Single(cues);
        Assert.Equal("Positioned", cue.Text);
    }

    [Fact]
    public void Parse_multi_line_cue()
    {
        var vtt = "WEBVTT\n\n00:00:01.000 --> 00:00:03.000\nLine one\nLine two\n";

        var cues = _format.Parse(vtt);

        var cue = Assert.Single(cues);
        Assert.Equal("Line one\nLine two", cue.Text);
    }
}
