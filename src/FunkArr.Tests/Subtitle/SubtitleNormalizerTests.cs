using FunkArr.Subtitle;

namespace FunkArr.Tests.Subtitle;

public class SubtitleNormalizerTests
{
    [Fact]
    public void ConvertVttToSrt_RemovesHeaderAndConvertsTimestamps()
    {
        var vtt = "WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHello World\n\n00:00:03.000 --> 00:00:04.000\nSecond cue\n";

        var result = SubtitleNormalizer.ConvertVttToSrt(vtt);

        Assert.Contains("1\r\n00:00:01,000 --> 00:00:02,000\r\nHello World", result);
        Assert.Contains("2\r\n00:00:03,000 --> 00:00:04,000\r\nSecond cue", result);
        Assert.DoesNotContain("WEBVTT", result);
    }

    [Fact]
    public void ConvertVttToSrt_SkipsNoteAndStyleBlocks()
    {
        var vtt = "WEBVTT\n\nNOTE This is a comment\n\nSTYLE\n::cue { color: white }\n\n00:00:01.000 --> 00:00:02.000\nHello\n";

        var result = SubtitleNormalizer.ConvertVttToSrt(vtt);

        Assert.DoesNotContain("NOTE", result);
        Assert.DoesNotContain("STYLE", result);
        Assert.Contains("Hello", result);
    }

    [Fact]
    public void ConvertTtmlToSrt_ExtractsTimedParagraphs()
    {
        var ttml = """
            <?xml version="1.0"?>
            <tt xml:lang="de">
            <body><div>
            <p begin="00:00:01.000" end="00:00:02.000">Hello</p>
            <p begin="00:00:03.000" end="00:00:04.000">World</p>
            </div></body>
            </tt>
            """;

        var result = SubtitleNormalizer.ConvertTtmlToSrt(ttml);

        Assert.Contains("1\r\n00:00:01,000 --> 00:00:02,000\r\nHello", result);
        Assert.Contains("2\r\n00:00:03,000 --> 00:00:04,000\r\nWorld", result);
    }

    [Fact]
    public void ConvertTtmlToSrt_StripsInlineHtmlTags()
    {
        var ttml = """<tt><body><div><p begin="00:00:01.000" end="00:00:02.000"><span>Hello <b>World</b></span></p></div></body></tt>""";

        var result = SubtitleNormalizer.ConvertTtmlToSrt(ttml);

        Assert.Contains("Hello World", result);
        Assert.DoesNotContain("<span>", result);
        Assert.DoesNotContain("<b>", result);
    }

    [Fact]
    public void ConvertTtmlToSrt_SkipsEmptyParagraphs()
    {
        var ttml = """<tt><body><div><p begin="00:00:01.000" end="00:00:02.000">   </p><p begin="00:00:03.000" end="00:00:04.000">Real text</p></div></body></tt>""";

        var result = SubtitleNormalizer.ConvertTtmlToSrt(ttml);

        Assert.StartsWith("1\r\n00:00:03,000", result);
    }

    [Fact]
    public void NormalizeTtmlTimestamp_DotToComma()
    {
        Assert.Equal("00:01:02,345", SubtitleNormalizer.NormalizeTtmlTimestamp("00:01:02.345"));
    }

    [Fact]
    public void NormalizeTtmlTimestamp_NoFraction_AppendsZeros()
    {
        Assert.Equal("00:01:02,000", SubtitleNormalizer.NormalizeTtmlTimestamp("00:01:02"));
    }

    [Fact]
    public void NormalizeTtmlTimestamp_AlreadyComma_Passthrough()
    {
        Assert.Equal("00:01:02,345", SubtitleNormalizer.NormalizeTtmlTimestamp("00:01:02,345"));
    }
}
