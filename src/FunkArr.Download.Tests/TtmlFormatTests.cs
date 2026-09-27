namespace FunkArr.Download.Tests;

public sealed class TtmlFormatTests
{
    private readonly TtmlFormat _format = new();

    [Fact]
    public void CanParse_detects_namespaced_ttml()
    {
        var content = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml" xml:lang="de">
              <tt:body><tt:div><tt:p begin="00:00:01.000" end="00:00:02.000"><tt:span>Hi</tt:span></tt:p></tt:div></tt:body>
            </tt:tt>
            """;

        Assert.True(_format.CanParse(content));
    }

    [Fact]
    public void CanParse_detects_plain_ttml()
    {
        var content = """
            <?xml version="1.0"?>
            <tt xmlns="http://www.w3.org/ns/ttml"><body><div><p begin="0s" end="1s">Hi</p></div></body></tt>
            """;

        Assert.True(_format.CanParse(content));
    }

    [Fact]
    public void CanParse_rejects_non_xml()
    {
        Assert.False(_format.CanParse("WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHi"));
    }

    [Fact]
    public void CanParse_rejects_non_tt_xml()
    {
        Assert.False(_format.CanParse("<html><body>Not TTML</body></html>"));
    }

    [Fact]
    public void Parse_ard_ebutt_basic_de()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml" xml:lang="de">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:02.200" end="00:00:03.800">
                    <tt:span>Hallo Welt</tt:span>
                  </tt:p>
                  <tt:p begin="00:00:05.000" end="00:00:07.500">
                    <tt:span>Zweiter Untertitel</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        Assert.Equal(2, cues.Count);
        Assert.Equal("Hallo Welt", cues[0].Text);
        Assert.Equal(TimeSpan.FromMilliseconds(2200), cues[0].Start);
        Assert.Equal(TimeSpan.FromMilliseconds(3800), cues[0].End);
        Assert.Equal("Zweiter Untertitel", cues[1].Text);
    }

    [Fact]
    public void Parse_orf_seconds_timestamps()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8" standalone="no"?>
            <tt xmlns="http://www.w3.org/ns/ttml">
              <head>TTML</head>
              <body>
                <div>
                  <p begin="1.10s" end="3.30s">Untertitel: AUDIO2</p>
                  <p begin="6.50s" end="8.70s">* dynamische Musik *</p>
                </div>
              </body>
            </tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        Assert.Equal(2, cues.Count);
        Assert.Equal("Untertitel: AUDIO2", cues[0].Text);
        Assert.Equal(TimeSpan.FromSeconds(1.1), cues[0].Start);
    }

    [Fact]
    public void Parse_nested_spans()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:01.000" end="00:00:03.000">
                    <tt:span style="s1">Erster </tt:span>
                    <tt:span style="s2">Teil</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        var cue = Assert.Single(cues);
        Assert.Contains("Erster", cue.Text);
        Assert.Contains("Teil", cue.Text);
    }

    [Fact]
    public void Parse_br_line_breaks()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:01.000" end="00:00:03.000">
                    <tt:span>Zeile eins</tt:span>
                    <tt:br/>
                    <tt:span>Zeile zwei</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        var cue = Assert.Single(cues);
        Assert.Contains("Zeile eins\nZeile zwei", cue.Text);
    }

    [Fact]
    public void Parse_skips_empty_paragraphs()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:02.000" end="00:00:04.000"><tt:span>  </tt:span></tt:p>
                  <tt:p begin="00:00:04.000" end="00:00:06.000"><tt:span>Real text</tt:span></tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        var cue = Assert.Single(cues);
        Assert.Equal("Real text", cue.Text);
    }

    [Fact]
    public void Parse_xml_entities_decoded()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml">
              <body><div><p begin="00:00:01.000" end="00:00:03.000">Tom &amp; Jerry &lt;3</p></div></body>
            </tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        var cue = Assert.Single(cues);
        Assert.Equal("Tom & Jerry <3", cue.Text);
    }

    [Fact]
    public void Parse_dur_attribute()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml">
              <body><div>
                <p begin="00:00:01.000" dur="00:00:02.000">Hello</p>
                <p begin="00:00:05.000" dur="00:00:03.000">World</p>
              </div></body>
            </tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        Assert.Equal(2, cues.Count);
        Assert.Equal(TimeSpan.FromSeconds(3), cues[0].End);
        Assert.Equal(TimeSpan.FromSeconds(8), cues[1].End);
    }

    [Fact]
    public void Parse_10h_offset_with_metadata()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml" xmlns:ebuttm="urn:ebu:tt:metadata">
              <tt:head>
                <tt:metadata>
                  <ebuttm:documentMetadata>
                    <ebuttm:documentStartOfProgramme>10:00:00.000</ebuttm:documentStartOfProgramme>
                  </ebuttm:documentMetadata>
                </tt:metadata>
              </tt:head>
              <tt:body>
                <tt:div>
                  <tt:p begin="10:00:34.480" end="10:00:36.920"><tt:span>Offset test</tt:span></tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        var cue = Assert.Single(cues);
        Assert.Equal(TimeSpan.FromMilliseconds(34480), cue.Start);
        Assert.Equal(TimeSpan.FromMilliseconds(36920), cue.End);
    }

    [Fact]
    public void Parse_auto_detect_offset_without_metadata()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="10:00:00.000" end="10:00:02.000"><tt:span>First</tt:span></tt:p>
                  <tt:p begin="10:00:05.000" end="10:00:08.000"><tt:span>Second</tt:span></tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        Assert.Equal(2, cues.Count);
        Assert.Equal(TimeSpan.Zero, cues[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(5), cues[1].Start);
    }

    [Fact]
    public void Parse_no_offset_when_near_zero()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:03.000" end="00:00:05.000"><tt:span>Near zero</tt:span></tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        var cue = Assert.Single(cues);
        Assert.Equal(TimeSpan.FromSeconds(3), cue.Start);
    }

    [Fact]
    public void Parse_negative_timestamps_clamped()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml" xmlns:ebuttm="urn:ebu:tt:metadata">
              <tt:head>
                <tt:metadata>
                  <ebuttm:documentMetadata>
                    <ebuttm:documentStartOfProgramme>10:00:05.000</ebuttm:documentStartOfProgramme>
                  </ebuttm:documentMetadata>
                </tt:metadata>
              </tt:head>
              <tt:body>
                <tt:div>
                  <tt:p begin="10:00:02.000" end="10:00:04.000"><tt:span>Before start</tt:span></tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        var cue = Assert.Single(cues);
        Assert.Equal(TimeSpan.Zero, cue.Start);
        Assert.Equal(TimeSpan.Zero, cue.End);
    }

    [Fact]
    public void Parse_xml_comment_preamble()
    {
        var ttml = """
            <!-- Profile: EBU-TT-D-Basic-DE --><tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:01.000" end="00:00:03.000"><tt:span>After comment</tt:span></tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        _format.CanParse(ttml);
        var cues = _format.Parse(ttml);

        var cue = Assert.Single(cues);
        Assert.Equal("After comment", cue.Text);
    }

    [Fact]
    public void ParseTimestamp_hhmmss_millis()
    {
        Assert.Equal(new TimeSpan(0, 1, 23, 45, 678), TtmlFormat.ParseTimestamp("01:23:45.678"));
    }

    [Fact]
    public void ParseTimestamp_seconds_only()
    {
        Assert.Equal(TimeSpan.FromSeconds(6.5), TtmlFormat.ParseTimestamp("6.50s"));
    }

    [Fact]
    public void ParseTimestamp_invalid_returns_zero()
    {
        Assert.Equal(TimeSpan.Zero, TtmlFormat.ParseTimestamp("invalid"));
    }

    [Fact]
    public void XDocument_cached_between_CanParse_and_Parse()
    {
        var ttml = """
            <?xml version="1.0"?>
            <tt xmlns="http://www.w3.org/ns/ttml"><body><div><p begin="0.5s" end="1s">Test</p></div></body></tt>
            """;

        Assert.True(_format.CanParse(ttml));
        var cues = _format.Parse(ttml);

        Assert.Single(cues);
    }
}
