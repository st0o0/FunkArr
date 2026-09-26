namespace FunkArr.Download.Tests;

public sealed class TtmlToSrtConverterTests
{
    [Fact]
    public void Convert_ard_ebutt_basic_de()
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

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:02,200 --> 00:00:03,800\nHallo Welt", srt);
        Assert.Contains("2\n00:00:05,000 --> 00:00:07,500\nZweiter Untertitel", srt);
    }

    [Fact]
    public void Convert_orf_plain_ttml_with_seconds_timestamps()
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

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:01,100 --> 00:00:03,300\nUntertitel: AUDIO2", srt);
        Assert.Contains("2\n00:00:06,500 --> 00:00:08,700\n* dynamische Musik *", srt);
    }

    [Fact]
    public void Convert_nested_spans()
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

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("Erster Teil", srt);
    }

    [Fact]
    public void Convert_br_line_breaks()
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

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("Zeile eins\nZeile zwei", srt);
    }

    [Fact]
    public void Convert_skips_empty_paragraphs()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:00.000" end="00:00:02.000">
                    <tt:span>.</tt:span>
                  </tt:p>
                  <tt:p begin="00:00:02.000" end="00:00:04.000">
                    <tt:span>  </tt:span>
                  </tt:p>
                  <tt:p begin="00:00:04.000" end="00:00:06.000">
                    <tt:span>Real text</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.StartsWith("1\n", srt);
        Assert.Contains(".", srt);
        Assert.Contains("Real text", srt);
    }

    [Fact]
    public void ParseTimestamp_hhmmss_millis()
    {
        var ts = TtmlToSrtConverter.ParseTimestamp("01:23:45.678");
        Assert.Equal(new TimeSpan(0, 1, 23, 45, 678), ts);
    }

    [Fact]
    public void ParseTimestamp_seconds_only()
    {
        var ts = TtmlToSrtConverter.ParseTimestamp("6.50s");
        Assert.Equal(TimeSpan.FromSeconds(6.5), ts);
    }

    [Fact]
    public void ParseTimestamp_invalid_returns_zero()
    {
        var ts = TtmlToSrtConverter.ParseTimestamp("invalid");
        Assert.Equal(TimeSpan.Zero, ts);
    }

    [Fact]
    public void Convert_xml_entities_decoded()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml">
              <body>
                <div>
                  <p begin="00:00:01.000" end="00:00:03.000">Tom &amp; Jerry &lt;3</p>
                </div>
              </body>
            </tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("Tom & Jerry <3", srt);
    }

    [Fact]
    public void Convert_dur_attribute_calculates_end_from_begin_plus_dur()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml">
              <body>
                <div>
                  <p begin="00:00:01.000" dur="00:00:02.000">Hello</p>
                  <p begin="00:00:05.000" dur="00:00:03.000">World</p>
                </div>
              </body>
            </tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:01,000 --> 00:00:03,000\nHello", srt);
        Assert.Contains("2\n00:00:05,000 --> 00:00:08,000\nWorld", srt);
    }

    [Fact]
    public void Convert_mixed_end_and_dur_attributes()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml">
              <body>
                <div>
                  <p begin="00:00:01.000" end="00:00:04.000">With end</p>
                  <p begin="00:00:05.000" dur="00:00:02.500">With dur</p>
                  <p begin="00:00:10.000" end="00:00:12.000">Back to end</p>
                </div>
              </body>
            </tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:01,000 --> 00:00:04,000\nWith end", srt);
        Assert.Contains("2\n00:00:05,000 --> 00:00:07,500\nWith dur", srt);
        Assert.Contains("3\n00:00:10,000 --> 00:00:12,000\nBack to end", srt);
    }

    [Fact]
    public void Convert_paragraphs_without_end_or_dur_returns_empty()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt xmlns="http://www.w3.org/ns/ttml">
              <body>
                <div>
                  <p begin="00:00:01.000">No end or dur</p>
                  <p begin="00:00:05.000">Also missing</p>
                </div>
              </body>
            </tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Equal("", srt);
    }

    [Fact]
    public void Convert_10h_offset_with_documentStartOfProgramme()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml"
                   xmlns:ebuttm="urn:ebu:tt:metadata">
              <tt:head>
                <tt:metadata>
                  <ebuttm:documentMetadata>
                    <ebuttm:documentStartOfProgramme>10:00:00.000</ebuttm:documentStartOfProgramme>
                  </ebuttm:documentMetadata>
                </tt:metadata>
              </tt:head>
              <tt:body>
                <tt:div>
                  <tt:p begin="10:00:34.480" end="10:00:36.920">
                    <tt:span>Hauptkommissarin Inga Luersen</tt:span>
                  </tt:p>
                  <tt:p begin="10:01:02.000" end="10:01:05.500">
                    <tt:span>Was ist passiert?</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:34,480 --> 00:00:36,920\nHauptkommissarin Inga Luersen", srt);
        Assert.Contains("2\n00:01:02,000 --> 00:01:05,500\nWas ist passiert?", srt);
    }

    [Fact]
    public void Convert_10h_offset_auto_detect_without_metadata()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="10:00:00.000" end="10:00:02.000">
                    <tt:span>Erster Untertitel</tt:span>
                  </tt:p>
                  <tt:p begin="10:00:05.000" end="10:00:08.000">
                    <tt:span>Zweiter Untertitel</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:00,000 --> 00:00:02,000\nErster Untertitel", srt);
        Assert.Contains("2\n00:00:05,000 --> 00:00:08,000\nZweiter Untertitel", srt);
    }

    [Fact]
    public void Convert_no_offset_when_timestamps_start_near_zero()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:03.000" end="00:00:05.000">
                    <tt:span>Near zero start</tt:span>
                  </tt:p>
                  <tt:p begin="00:05:00.000" end="00:05:03.000">
                    <tt:span>Five minutes in</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:03,000 --> 00:00:05,000\nNear zero start", srt);
        Assert.Contains("2\n00:05:00,000 --> 00:05:03,000\nFive minutes in", srt);
    }

    [Fact]
    public void Convert_20h_offset()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml"
                   xmlns:ebuttm="urn:ebu:tt:metadata">
              <tt:head>
                <tt:metadata>
                  <ebuttm:documentMetadata>
                    <ebuttm:documentStartOfProgramme>20:00:00.000</ebuttm:documentStartOfProgramme>
                  </ebuttm:documentMetadata>
                </tt:metadata>
              </tt:head>
              <tt:body>
                <tt:div>
                  <tt:p begin="20:00:10.000" end="20:00:13.000">
                    <tt:span>Twenty hour offset</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:10,000 --> 00:00:13,000\nTwenty hour offset", srt);
    }

    [Fact]
    public void Convert_offset_clamps_negative_timestamps()
    {
        var ttml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tt:tt xmlns:tt="http://www.w3.org/ns/ttml"
                   xmlns:ebuttm="urn:ebu:tt:metadata">
              <tt:head>
                <tt:metadata>
                  <ebuttm:documentMetadata>
                    <ebuttm:documentStartOfProgramme>10:00:05.000</ebuttm:documentStartOfProgramme>
                  </ebuttm:documentMetadata>
                </tt:metadata>
              </tt:head>
              <tt:body>
                <tt:div>
                  <tt:p begin="10:00:02.000" end="10:00:04.000">
                    <tt:span>Before programme start</tt:span>
                  </tt:p>
                  <tt:p begin="10:00:10.000" end="10:00:12.000">
                    <tt:span>After programme start</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("00:00:00,000 --> 00:00:00,000\nBefore programme start", srt);
        Assert.Contains("00:00:05,000 --> 00:00:07,000\nAfter programme start", srt);
    }

    [Fact]
    public void Convert_xml_comment_preamble()
    {
        var ttml = """
            <!-- Profile: EBU-TT-D-Basic-DE --><tt:tt xmlns:tt="http://www.w3.org/ns/ttml">
              <tt:body>
                <tt:div>
                  <tt:p begin="00:00:01.000" end="00:00:03.000">
                    <tt:span>After comment</tt:span>
                  </tt:p>
                </tt:div>
              </tt:body>
            </tt:tt>
            """;

        var srt = TtmlToSrtConverter.Convert(ttml);

        Assert.Contains("1\n00:00:01,000 --> 00:00:03,000\nAfter comment", srt);
    }
}
