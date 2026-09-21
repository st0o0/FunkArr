using Newtonsoft.Json;
using FunkArr.Persistence.Events.ScoringHistory;
using FunkArr.Tests.Shared;
using static VerifyXunit.Verifier;

namespace FunkArr.Persistence.Tests.Events.ScoringHistory;

public sealed class PersistedHistoryStateVerifyTests
{
    private static readonly Guid TestRequestId1 = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid TestRequestId2 = new("c3d4e5f6-a7b8-9012-cdef-123456789012");
    private static readonly DateTimeOffset TestTimestamp = new(2024, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public Task PersistedHistoryState_shape()
    {
        var state = new PersistedHistoryState(
        [
            new HistoryRecorded(
                TestRequestId1, PersistedSearchSource.Sonarr, "Tatort",
                TestTimestamp, 10, 3, 2,
                [TestItemTraceBuilder.CreateSampleTrace()]),
            new HistoryRecorded(
                TestRequestId2, PersistedSearchSource.Radarr, "Film",
                TestTimestamp.AddHours(1), 5, 1, 0, []),
        ]);
        var json = JsonConvert.SerializeObject(state, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void PersistedHistoryState_roundtrip()
    {
        var original = new PersistedHistoryState(
        [
            new HistoryRecorded(
                TestRequestId1, PersistedSearchSource.Sonarr, "Tatort",
                TestTimestamp, 10, 3, 2,
                [TestItemTraceBuilder.CreateSampleTrace()]),
        ]);

        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<PersistedHistoryState>(json)!;

        Assert.Single(result.Entries);
        Assert.Equal(original.Entries[0].RequestId, result.Entries[0].RequestId);
        Assert.Equal(original.Entries[0].Source, result.Entries[0].Source);
        Assert.Equal(original.Entries[0].Query, result.Entries[0].Query);
        Assert.Equal(original.Entries[0].CandidateCount, result.Entries[0].CandidateCount);
        Assert.Single(result.Entries[0].ItemTraces);
    }
}
