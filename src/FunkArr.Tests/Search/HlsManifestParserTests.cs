using FunkArr.Search;
using FunkArr.Search.Quality;
using FunkArr.Shared.Models;

namespace FunkArr.Tests.Search;

public class HlsManifestParserTests
{
    [Fact]
    public void Parse_SingleVariant_ExtractsResolutionAndBandwidth()
    {
        var manifest = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=5000000,RESOLUTION=1920x1080
            stream_1080.m3u8
            """;

        var result = HlsManifestParser.Parse(manifest, durationSeconds: 3600);

        Assert.NotNull(result);
        Assert.Equal(new Resolution(1920, 1080), result.Resolution);
        Assert.Equal(5000, result.BitrateKbps);
        Assert.Equal(ProbeSource.HlsManifest, result.ProbeSource);
    }

    [Fact]
    public void Parse_MultipleVariants_PicksHighestBandwidth()
    {
        var manifest = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=800000,RESOLUTION=640x360
            stream_360.m3u8
            #EXT-X-STREAM-INF:BANDWIDTH=2500000,RESOLUTION=1280x720
            stream_720.m3u8
            #EXT-X-STREAM-INF:BANDWIDTH=5000000,RESOLUTION=1920x1080
            stream_1080.m3u8
            """;

        var result = HlsManifestParser.Parse(manifest, durationSeconds: 1800);

        Assert.NotNull(result);
        Assert.Equal(new Resolution(1920, 1080), result.Resolution);
        Assert.Equal(5000, result.BitrateKbps);
    }

    [Fact]
    public void Parse_NoResolution_EstimatesFromBandwidth()
    {
        var manifest = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=5000000
            stream.m3u8
            """;

        var result = HlsManifestParser.Parse(manifest, durationSeconds: 3600);

        Assert.NotNull(result);
        Assert.Equal(new Resolution(1920, 1080), result.Resolution);
    }

    [Fact]
    public void Parse_EmptyManifest_ReturnsNull()
    {
        var manifest = "#EXTM3U\n";

        var result = HlsManifestParser.Parse(manifest, durationSeconds: 3600);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_CalculatesFileSize()
    {
        var manifest = """
            #EXTM3U
            #EXT-X-STREAM-INF:BANDWIDTH=4000000,RESOLUTION=1920x1080
            stream.m3u8
            """;

        var result = HlsManifestParser.Parse(manifest, durationSeconds: 3600);

        Assert.NotNull(result);
        Assert.Equal(3600L * 4000000 / 8, result.FileSize);
    }
}
