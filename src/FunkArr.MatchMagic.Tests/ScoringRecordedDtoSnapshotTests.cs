using System.Text.Json;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.MatchHistory;

namespace FunkArr.MatchMagic.Tests;

public sealed class ScoringRecordedSerializerTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Roundtrip_serialization_preserves_all_fields()
    {
        var original = CreateEvent();
        var json = JsonSerializer.SerializeToUtf8Bytes(original, _options);
        var deserialized = JsonSerializer.Deserialize<ScoringRecorded>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.RequestId, deserialized.RequestId);
        Assert.Equal(original.Source, deserialized.Source);
        Assert.Equal(original.Query, deserialized.Query);
        Assert.Equal(original.Timestamp, deserialized.Timestamp);
        Assert.Equal(original.CandidateCount, deserialized.CandidateCount);
        Assert.Equal(original.MatchedCount, deserialized.MatchedCount);
        Assert.Equal(original.ItemTraces.Length, deserialized.ItemTraces.Length);
    }

    [Fact]
    public void Roundtrip_preserves_nested_trace_data()
    {
        var original = CreateEvent();
        var json = JsonSerializer.SerializeToUtf8Bytes(original, _options);
        var deserialized = JsonSerializer.Deserialize<ScoringRecorded>(json, _options)!;

        var trace = deserialized.ItemTraces[0];
        Assert.Equal("Tatort: Die goldene Zeit", trace.CandidateTitle);
        Assert.True(trace.Matched);
        Assert.Equal(0.95, trace.Score);
        Assert.NotNull(trace.Identification);
        Assert.Equal("01", trace.Identification.Season);
        Assert.Single(trace.RuleTraces);
        Assert.Equal("season-episode", trace.RuleTraces[0].RuleId);
        Assert.Equal(RuleOutcome.Matched, trace.RuleTraces[0].Outcome);
        Assert.NotNull(trace.RuleTraces[0].FilterTrace);
        Assert.Equal(2, trace.RuleTraces[0].FilterTrace!.Nodes.Length);
    }

    private static ScoringRecorded CreateEvent() => new(
        RequestId: Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
        Source: "sonarr",
        Query: "Tatort",
        Timestamp: DateTimeOffset.Parse("2026-08-31T14:23:00+00:00"),
        CandidateCount: 2,
        MatchedCount: 1,
        ItemTraces:
        [
            new ItemTrace(
                CandidateTitle: "Tatort: Die goldene Zeit",
                CandidateTopic: "Tatort",
                CandidateChannel: "ARD",
                CandidateDuration: 5400,
                CandidateQuality: 720,
                CandidateDescription: "Kommissarin Lena Odenthal ermittelt",
                CandidateTimestamp: 1719331200,
                Matched: true,
                Score: 0.95,
                MatchedRuleId: "season-episode",
                Identification: new TracedIdentification("01", "05", null),
                RuleTraces:
                [
                    new RuleTrace(
                        RuleId: "season-episode",
                        Priority: 0,
                        Outcome: RuleOutcome.Matched,
                        FilterTrace: new FilterGroupTrace("All", true,
                        [
                            new FilterNodeTrace("Channel", "Eq", "ARD", "ARD", true, false, null),
                            new FilterNodeTrace("Duration", "GreaterThan", "30", "90", true, false, null)
                        ]),
                        IdentificationTrace: new IdentificationTrace("RegexCapture", true, null))
                ])
        ]);
}
