using System.Text.Encodings.Web;
using System.Text.Json;
using FunkArr.Persistence.MatchHistory;

namespace FunkArr.MatchMagic.Tests;

public sealed class ScoringRecordedDtoSnapshotTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public void Serialization_matches_golden_file()
    {
        var dto = CreateFullyPopulatedDto();
        var serialized = JsonSerializer.Serialize(dto, _options);
        var goldenPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", "ScoringRecordedDto_v1.json");
        var expected = File.ReadAllText(goldenPath);
        Assert.Equal(Normalize(expected), Normalize(serialized));
    }

    [Fact]
    public void Roundtrip_deserialization_produces_identical_output()
    {
        var goldenPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", "ScoringRecordedDto_v1.json");
        var json = File.ReadAllText(goldenPath);
        var deserialized = JsonSerializer.Deserialize<ScoringRecordedDto>(json, _options);
        Assert.NotNull(deserialized);
        var reserialized = JsonSerializer.Serialize(deserialized, _options);
        Assert.Equal(Normalize(json), Normalize(reserialized));
    }

    private static string Normalize(string s) => s.ReplaceLineEndings("\n").Trim();

    private static ScoringRecordedDto CreateFullyPopulatedDto() => new()
    {
        Version = 1,
        RequestId = "550e8400-e29b-41d4-a716-446655440000",
        Source = "sonarr",
        Query = "Tatort",
        Timestamp = "2026-08-31T14:23:00+00:00",
        CandidateCount = 2,
        MatchedCount = 1,
        ItemTraces =
        [
            new ItemTraceDto
            {
                CandidateTitle = "Tatort: Die goldene Zeit (S01/E05)",
                CandidateTopic = "Tatort",
                CandidateChannel = "ARD",
                CandidateDuration = 5400,
                CandidateQuality = 720,
                CandidateDescription = "Kommissarin Lena Odenthal ermittelt",
                CandidateTimestamp = 1719331200,
                Matched = true,
                Score = 0.95,
                MatchedRuleId = "season-episode",
                Identification = new TracedIdentificationDto
                {
                    Season = "01",
                    Episode = "05",
                    Title = null
                },
                RuleTraces =
                [
                    new RuleTraceDto
                    {
                        RuleId = "season-episode",
                        Priority = 0,
                        Outcome = "Matched",
                        FilterTrace = new FilterGroupTraceDto
                        {
                            Operator = "All",
                            Passed = true,
                            Nodes =
                            [
                                new FilterNodeTraceDto
                                {
                                    NodeType = "condition",
                                    Field = "Channel",
                                    Op = "Eq",
                                    ExpectedValue = "ARD",
                                    ActualValue = "ARD",
                                    Passed = true,
                                    Skipped = false,
                                    Group = null
                                },
                                new FilterNodeTraceDto
                                {
                                    NodeType = "condition",
                                    Field = "Duration",
                                    Op = "GreaterThan",
                                    ExpectedValue = "30",
                                    ActualValue = "90",
                                    Passed = true,
                                    Skipped = false,
                                    Group = null
                                }
                            ]
                        },
                        IdentificationTrace = new IdentificationTraceDto
                        {
                            Strategy = "RegexCapture",
                            Attempted = true,
                            Detail = null
                        }
                    }
                ]
            },
            new ItemTraceDto
            {
                CandidateTitle = "Tatort: Making-Of",
                CandidateTopic = "Tatort",
                CandidateChannel = "ARD",
                CandidateDuration = 900,
                CandidateQuality = 480,
                CandidateDescription = null,
                CandidateTimestamp = 1719244800,
                Matched = false,
                Score = 0,
                MatchedRuleId = null,
                Identification = null,
                RuleTraces =
                [
                    new RuleTraceDto
                    {
                        RuleId = "season-episode",
                        Priority = 0,
                        Outcome = "FilterFailed",
                        FilterTrace = new FilterGroupTraceDto
                        {
                            Operator = "All",
                            Passed = false,
                            Nodes =
                            [
                                new FilterNodeTraceDto
                                {
                                    NodeType = "condition",
                                    Field = "Channel",
                                    Op = "Eq",
                                    ExpectedValue = "ARD",
                                    ActualValue = "ARD",
                                    Passed = true,
                                    Skipped = false,
                                    Group = null
                                },
                                new FilterNodeTraceDto
                                {
                                    NodeType = "condition",
                                    Field = "Duration",
                                    Op = "GreaterThan",
                                    ExpectedValue = "30",
                                    ActualValue = "15",
                                    Passed = false,
                                    Skipped = false,
                                    Group = null
                                }
                            ]
                        },
                        IdentificationTrace = new IdentificationTraceDto
                        {
                            Strategy = null,
                            Attempted = false,
                            Detail = null
                        }
                    }
                ]
            }
        ]
    };
}
