using FunkArr.Indexer;
using FunkArr.Shared.Models;

namespace FunkArr.Tests.Contracts;

public sealed class NewznabContractSpec
{
    [Fact(Timeout = 5000)]
    public Task CapsResponse_WireFormat()
    {
        var xml = NewznabXmlBuilder.BuildCapsResponse("http://localhost:5000/api");
        return Verify(xml).ScrubEmptyLines();
    }

    [Fact(Timeout = 5000)]
    public Task TvSearchResponse_WireFormat()
    {
        var results = new List<NewznabResult>
        {
            new()
            {
                Title = "Tatort.S2026E10.GERMAN.1080p.WEB.h264-FA",
                DownloadUrl = "http://localhost:5000/api?t=get&id=abc",
                SizeBytes = 1_500_000_000,
                PublishDate = new DateTimeOffset(2026, 6, 1, 20, 15, 0, TimeSpan.Zero),
                Category = "5040",
                Guid = "abc-123",
                QualityInfo = new QualityInfo
                {
                    Resolution = new Resolution(1920, 1080),
                    Codec = "h264",
                    FileSize = 1_500_000_000,
                    ProbeSource = ProbeSource.ContainerHeader,
                },
                TvdbId = 12345,
                Season = 2026,
                Episode = 10,
            },
        };

        var xml = NewznabXmlBuilder.BuildSearchResponse(results);
        return Verify(xml).ScrubEmptyLines();
    }

    [Fact(Timeout = 5000)]
    public Task EmptySearchResponse_WireFormat()
    {
        var xml = NewznabXmlBuilder.BuildSearchResponse([]);
        return Verify(xml).ScrubEmptyLines();
    }

    [Fact(Timeout = 5000)]
    public Task ErrorResponse_WireFormat()
    {
        var xml = NewznabXmlBuilder.BuildErrorResponse(100, "Incorrect parameter");
        return Verify(xml).ScrubEmptyLines();
    }
}
