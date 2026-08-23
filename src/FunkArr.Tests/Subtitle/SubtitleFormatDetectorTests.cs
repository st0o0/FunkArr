using FunkArr.Subtitle;

namespace FunkArr.Tests.Subtitle;

public class SubtitleFormatDetectorTests
{
    [Fact]
    public void Detect_WebVttHeader_ReturnsWebVtt()
    {
        var result = SubtitleFormatDetector.Detect("WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHello");

        Assert.Equal(SubtitleFormat.WebVtt, result);
    }

    [Fact]
    public void Detect_WebVttWithBom_ReturnsWebVtt()
    {
        var result = SubtitleFormatDetector.Detect("﻿WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHello");

        Assert.Equal(SubtitleFormat.WebVtt, result);
    }

    [Fact]
    public void Detect_XmlDeclarationWithTtml_ReturnsTtml()
    {
        var result = SubtitleFormatDetector.Detect("<?xml version=\"1.0\"?>\n<tt xml:lang=\"de\">");

        Assert.Equal(SubtitleFormat.Ttml, result);
    }

    [Fact]
    public void Detect_TtTagWithoutXmlDeclaration_ReturnsTtml()
    {
        var result = SubtitleFormatDetector.Detect("<tt xml:lang=\"de\">\n<body><p begin=\"00:00:01\">");

        Assert.Equal(SubtitleFormat.Ttml, result);
    }

    [Fact]
    public void Detect_SrtContent_ReturnsSrt()
    {
        var result = SubtitleFormatDetector.Detect("1\n00:00:01,000 --> 00:00:02,000\nHello\n");

        Assert.Equal(SubtitleFormat.Srt, result);
    }

    [Fact]
    public void Detect_EmptyContent_ReturnsUnknown()
    {
        var result = SubtitleFormatDetector.Detect("");

        Assert.Equal(SubtitleFormat.Unknown, result);
    }

    [Fact]
    public void Detect_UnrecognizedContent_ReturnsUnknown()
    {
        var result = SubtitleFormatDetector.Detect("This is just random text with no subtitle markers");

        Assert.Equal(SubtitleFormat.Unknown, result);
    }

    [Fact]
    public void Detect_EmptyBytes_ReturnsUnknown()
    {
        var result = SubtitleFormatDetector.Detect(Array.Empty<byte>());

        Assert.Equal(SubtitleFormat.Unknown, result);
    }
}
