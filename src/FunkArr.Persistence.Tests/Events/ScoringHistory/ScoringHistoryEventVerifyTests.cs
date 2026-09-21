using Newtonsoft.Json;
using FunkArr.Persistence.Events.ScoringHistory;
using FunkArr.Tests.Shared;
using static VerifyXunit.Verifier;

namespace FunkArr.Persistence.Tests.Events.ScoringHistory;

public sealed class ScoringHistoryEventVerifyTests
{
    private static readonly Guid TestRequestId = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly DateTimeOffset TestTimestamp = new(2024, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public Task HistoryRecorded_shape()
    {
        var evt = new HistoryRecorded(
            TestRequestId,
            PersistedSearchSource.Sonarr,
            "Tatort",
            TestTimestamp,
            CandidateCount: 10,
            MatchedCount: 3,
            EnrichedCount: 2,
            ItemTraces: [TestItemTraceBuilder.CreateSampleTrace()]);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void HistoryRecorded_roundtrip()
    {
        var original = new HistoryRecorded(
            TestRequestId,
            PersistedSearchSource.Sonarr,
            "Tatort",
            TestTimestamp,
            CandidateCount: 10,
            MatchedCount: 3,
            EnrichedCount: 2,
            ItemTraces: [TestItemTraceBuilder.CreateSampleTrace()]);

        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<HistoryRecorded>(json)!;

        Assert.Equal(original.RequestId, result.RequestId);
        Assert.Equal(original.Source, result.Source);
        Assert.Equal(original.Query, result.Query);
        Assert.Equal(original.Timestamp, result.Timestamp);
        Assert.Equal(original.CandidateCount, result.CandidateCount);
        Assert.Equal(original.MatchedCount, result.MatchedCount);
        Assert.Equal(original.EnrichedCount, result.EnrichedCount);
        Assert.Single(result.ItemTraces);
        Assert.Equal(original.ItemTraces[0].CandidateTitle, result.ItemTraces[0].CandidateTitle);
        Assert.Equal(original.ItemTraces[0].Score, result.ItemTraces[0].Score);
    }
}
