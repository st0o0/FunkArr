using FunkArr.Api.Extensions;
using FunkArr.Messages.Enrichment;
using ApiModels = FunkArr.Api.Models;
using MsgHistory = FunkArr.Messages.Scoring.History;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Tests.Extensions;

public sealed class ScoringMappingExtensionsTests
{
    [Fact]
    public void ItemTrace_maps_candidate_fields()
    {
        var candidate = new MsgScoring.ScoreCandidate("Tagesschau", "Nachrichten", "ARD", 900, 1080, "Daily news", 1725300000);
        var trace = new MsgHistory.ItemTrace(candidate, true, 0.95, "rule-1", null, []);

        var result = trace.ToApi();

        Assert.Equal("Tagesschau", result.Candidate.Title);
        Assert.Equal("Nachrichten", result.Candidate.Topic);
        Assert.Equal("ARD", result.Candidate.Channel);
        Assert.Equal(900, result.Candidate.Duration);
        Assert.Equal(1080, result.Candidate.Quality);
        Assert.Equal("Daily news", result.Candidate.Description);
        Assert.Equal(1725300000, result.Candidate.Timestamp);
    }

    [Fact]
    public void ItemTrace_maps_matched_and_score()
    {
        var candidate = new MsgScoring.ScoreCandidate("Test", "Topic", "ZDF", 600, 720, null, 0);
        var trace = new MsgHistory.ItemTrace(candidate, false, 0.42, null, null, []);

        var result = trace.ToApi();

        Assert.False(result.Matched);
        Assert.Equal(0.42, result.Score);
        Assert.Null(result.MatchedRuleId);
    }

    [Fact]
    public void ItemTrace_maps_identification_when_present()
    {
        var candidate = new MsgScoring.ScoreCandidate("Test", "Topic", "ARD", 600, 720, null, 0);
        var identification = new MsgHistory.TracedIdentification("S01", "E05", "Episode Title");
        var trace = new MsgHistory.ItemTrace(candidate, true, 0.9, "rule-1", identification, []);

        var result = trace.ToApi();

        Assert.NotNull(result.Identification);
        Assert.Equal("S01", result.Identification.Season);
        Assert.Equal("E05", result.Identification.Episode);
        Assert.Equal("Episode Title", result.Identification.Title);
    }

    [Fact]
    public void ItemTrace_maps_null_identification()
    {
        var candidate = new MsgScoring.ScoreCandidate("Test", "Topic", "ARD", 600, 720, null, 0);
        var trace = new MsgHistory.ItemTrace(candidate, false, 0.0, null, null, []);

        var result = trace.ToApi();

        Assert.Null(result.Identification);
    }

    [Fact]
    public void ItemTrace_maps_enrichment_trace_when_present()
    {
        var candidate = new MsgScoring.ScoreCandidate("Test", "Topic", "ARD", 600, 720, null, 0);
        var enrichment = new MsgHistory.EnrichmentTrace(
            MatchMethod.TitleMatch, 0.85f, true,
            ResolvedSeason: "1", ResolvedEpisode: "3",
            ResolvedTitle: "Resolved", ResolvedYear: 2024,
            DaysDiff: 2, Detail: "matched by title");
        var trace = new MsgHistory.ItemTrace(candidate, true, 0.9, "rule-1", null, [], enrichment);

        var result = trace.ToApi();

        Assert.NotNull(result.EnrichmentTrace);
        Assert.Equal(ApiModels.MatchMethod.TitleMatch, result.EnrichmentTrace.Method);
        Assert.Equal(0.85f, result.EnrichmentTrace.Confidence);
        Assert.True(result.EnrichmentTrace.Enriched);
        Assert.Equal("1", result.EnrichmentTrace.ResolvedSeason);
        Assert.Equal("3", result.EnrichmentTrace.ResolvedEpisode);
        Assert.Equal("Resolved", result.EnrichmentTrace.ResolvedTitle);
        Assert.Equal(2024, result.EnrichmentTrace.ResolvedYear);
        Assert.Equal(2, result.EnrichmentTrace.DaysDiff);
        Assert.Equal("matched by title", result.EnrichmentTrace.Detail);
    }

    [Fact]
    public void ItemTrace_maps_null_enrichment_trace()
    {
        var candidate = new MsgScoring.ScoreCandidate("Test", "Topic", "ARD", 600, 720, null, 0);
        var trace = new MsgHistory.ItemTrace(candidate, false, 0.0, null, null, []);

        var result = trace.ToApi();

        Assert.Null(result.EnrichmentTrace);
    }

    [Fact]
    public void RuleTrace_maps_with_filter_and_identification_traces()
    {
        var filterNode = new MsgHistory.FilterNodeTrace("Title", "Contains", "Tagesschau", "Tagesschau 20:00", true, false, null);
        var filterTrace = new MsgHistory.FilterGroupTrace(MsgScoring.FilterGroupOp.All, true, [filterNode]);
        var idTrace = new MsgHistory.IdentificationTrace(
            MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber, true, null);
        var ruleTrace = new MsgHistory.RuleTrace("rule-1", 10, MsgHistory.RuleOutcome.Matched, filterTrace, idTrace);

        var result = ruleTrace.ToApi();

        Assert.Equal("rule-1", result.RuleId);
        Assert.Equal(10, result.Priority);
        Assert.Equal(ApiModels.RuleOutcome.Matched, result.Outcome);
        Assert.NotNull(result.FilterTrace);
        Assert.Equal("All", result.FilterTrace.Operator);
        Assert.True(result.FilterTrace.Passed);
        var node = Assert.Single(result.FilterTrace.Nodes);
        Assert.Equal("Title", node.Field);
        Assert.Equal("Contains", node.Op);
        Assert.Equal("Tagesschau", node.ExpectedValue);
        Assert.Equal("Tagesschau 20:00", node.ActualValue);
        Assert.True(node.Passed);
        Assert.False(node.Skipped);
        Assert.Null(node.Group);
        Assert.NotNull(result.IdentificationTrace);
        Assert.Equal("SeasonAndEpisodeNumber", result.IdentificationTrace.Strategy);
        Assert.True(result.IdentificationTrace.Attempted);
        Assert.Null(result.IdentificationTrace.Detail);
    }

    [Fact]
    public void RuleTrace_maps_null_traces()
    {
        var ruleTrace = new MsgHistory.RuleTrace("rule-2", 5, MsgHistory.RuleOutcome.FilterFailed, null, null);

        var result = ruleTrace.ToApi();

        Assert.Equal("rule-2", result.RuleId);
        Assert.Equal(5, result.Priority);
        Assert.Equal(ApiModels.RuleOutcome.FilterFailed, result.Outcome);
        Assert.Null(result.FilterTrace);
        Assert.Null(result.IdentificationTrace);
    }

    [Fact]
    public void FilterGroupTrace_maps_operator_as_string_and_nodes()
    {
        var node1 = new MsgHistory.FilterNodeTrace("Channel", "Eq", "ARD", "ARD", true, false, null);
        var node2 = new MsgHistory.FilterNodeTrace("Duration", "GreaterThan", "300", "600", true, false, null);
        var trace = new MsgHistory.FilterGroupTrace(MsgScoring.FilterGroupOp.Any, true, [node1, node2]);

        var result = trace.ToApi();

        Assert.Equal("Any", result.Operator);
        Assert.True(result.Passed);
        Assert.Equal(2, result.Nodes.Length);
        Assert.Equal("Channel", result.Nodes[0].Field);
        Assert.Equal("Duration", result.Nodes[1].Field);
    }

    [Fact]
    public void RuleOutcome_Matched_maps_correctly()
    {
        var result = MsgHistory.RuleOutcome.Matched.ToApi();

        Assert.Equal(ApiModels.RuleOutcome.Matched, result);
    }

    [Fact]
    public void RuleOutcome_FilterFailed_maps_correctly()
    {
        var result = MsgHistory.RuleOutcome.FilterFailed.ToApi();

        Assert.Equal(ApiModels.RuleOutcome.FilterFailed, result);
    }

    [Fact]
    public void RuleOutcome_IdentificationFailed_maps_correctly()
    {
        var result = MsgHistory.RuleOutcome.IdentificationFailed.ToApi();

        Assert.Equal(ApiModels.RuleOutcome.IdentificationFailed, result);
    }
}
