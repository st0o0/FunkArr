using FunkArr.Download;

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
}
