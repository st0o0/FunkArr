using FunkArr.Api.Extensions;
using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;
using ApiModels = FunkArr.Api.Models;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Tests.Extensions;

public sealed class RuleSetMappingExtensionsTests
{
    [Fact]
    public void RuleSetDetail_maps_identity_with_external_ids()
    {
        var ids = new ExternalIds(12345, "tt1234567", 99);
        var identity = new RuleSetDetailResult.RuleSetIdentity("Tagesschau", ["TS", "Tagesschau 20"], ids);
        var source = new RuleSetDetailResult.RuleSetSource(null, null, null, null);
        var enrichment = CreateEnrichmentConfig();
        var msg = new RuleSetDetailResult("tagesschau", identity, source, 0.8f, [], enrichment);

        var result = msg.ToApi();

        Assert.Equal("tagesschau", result.RuleSetId);
        Assert.Equal("Tagesschau", result.Identity.Topic);
        Assert.Equal(2, result.Identity.Aliases.Length);
        Assert.Equal("TS", result.Identity.Aliases[0]);
        Assert.Equal("Tagesschau 20", result.Identity.Aliases[1]);
        Assert.Equal(12345, result.Identity.TvdbId);
        Assert.Equal("tt1234567", result.Identity.ImdbId);
        Assert.Equal(99, result.Identity.TmdbId);
    }

    [Fact]
    public void RuleSetDetail_maps_source_paths()
    {
        var ids = new ExternalIds(null, null, null);
        var identity = new RuleSetDetailResult.RuleSetIdentity("Test", [], ids);
        var communityModified = new DateTime(2025, 6, 15, 10, 0, 0);
        var localModified = new DateTime(2025, 7, 1, 12, 0, 0);
        var source = new RuleSetDetailResult.RuleSetSource("community/test.json", "local/test.json", communityModified, localModified);
        var enrichment = CreateEnrichmentConfig();
        var msg = new RuleSetDetailResult("test", identity, source, 0.9f, [], enrichment);

        var result = msg.ToApi();

        Assert.Equal("community/test.json", result.Source.CommunityPath);
        Assert.Equal("local/test.json", result.Source.LocalPath);
        Assert.Equal(communityModified, result.Source.CommunityModified);
        Assert.Equal(localModified, result.Source.LocalModified);
    }

    [Fact]
    public void RuleSetDetail_maps_rules_with_filters()
    {
        var filters = new MsgScoring.FilterGroupOutput(
            All: [new MsgScoring.FilterConditionOutput(MsgScoring.FilterField.Channel, MsgScoring.FilterOp.Eq, "ARD")],
            Any: null,
            Not: null);
        var titleRules = new[]
        {
            new MsgScoring.TitleRuleOutput(MsgScoring.TitlePartType.Static, Value: "Tagesschau")
        };
        var rule = new RuleSetDetailRule(
            "rule-1", 10, 0.95f, MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber,
            @"S(\d+)", @"E(\d+)", 1, filters, titleRules);

        var ids = new ExternalIds(null, null, null);
        var identity = new RuleSetDetailResult.RuleSetIdentity("Test", [], ids);
        var source = new RuleSetDetailResult.RuleSetSource(null, null, null, null);
        var enrichment = CreateEnrichmentConfig();
        var msg = new RuleSetDetailResult("test", identity, source, 0.8f, [rule], enrichment);

        var result = msg.ToApi();

        var mappedRule = Assert.Single(result.Rules);
        Assert.Equal("rule-1", mappedRule.Id);
        Assert.Equal(10, mappedRule.Priority);
        Assert.Equal(0.95f, mappedRule.Confidence);
        Assert.Equal(ApiModels.IdentificationStrategy.SeasonAndEpisodeNumber, mappedRule.Strategy);
        Assert.Equal(@"S(\d+)", mappedRule.SeasonRegex);
        Assert.Equal(@"E(\d+)", mappedRule.EpisodeRegex);
        Assert.Equal(1, mappedRule.CaptureGroup);

        Assert.NotNull(mappedRule.Filters);
        Assert.NotNull(mappedRule.Filters.All);
        var condition = Assert.Single(mappedRule.Filters.All);
        Assert.Equal(ApiModels.FilterField.Channel, condition.Field);
        Assert.Equal(ApiModels.FilterOp.Eq, condition.Op);
        Assert.Equal("ARD", condition.Value);

        Assert.NotNull(mappedRule.TitleRules);
        var titleRule = Assert.Single(mappedRule.TitleRules);
        Assert.Equal(ApiModels.TitlePartType.Static, titleRule.Type);
        Assert.Equal("Tagesschau", titleRule.Value);
    }

    [Fact]
    public void FilterGroupOutput_maps_null_returns_null()
    {
        MsgScoring.FilterGroupOutput? input = null;

        var result = input.ToApi();

        Assert.Null(result);
    }

    [Fact]
    public void FilterGroupOutput_maps_all_conditions()
    {
        var input = new MsgScoring.FilterGroupOutput(
            All: [new MsgScoring.FilterConditionOutput(MsgScoring.FilterField.Title, MsgScoring.FilterOp.Contains, "Tatort")],
            Any: [new MsgScoring.FilterConditionOutput(MsgScoring.FilterField.Channel, MsgScoring.FilterOp.Eq, "ARD")],
            Not: [new MsgScoring.FilterConditionOutput(MsgScoring.FilterField.Description, MsgScoring.FilterOp.Regex, "Wiederholung")]);

        var result = input.ToApi();

        Assert.NotNull(result);
        Assert.NotNull(result.All);
        var a = Assert.Single(result.All);
        Assert.Equal(ApiModels.FilterField.Title, a.Field);
        Assert.Equal(ApiModels.FilterOp.Contains, a.Op);
        Assert.Equal("Tatort", a.Value);

        Assert.NotNull(result.Any);
        var b = Assert.Single(result.Any);
        Assert.Equal(ApiModels.FilterField.Channel, b.Field);
        Assert.Equal(ApiModels.FilterOp.Eq, b.Op);
        Assert.Equal("ARD", b.Value);

        Assert.NotNull(result.Not);
        var c = Assert.Single(result.Not);
        Assert.Equal(ApiModels.FilterField.Description, c.Field);
        Assert.Equal(ApiModels.FilterOp.Regex, c.Op);
        Assert.Equal("Wiederholung", c.Value);
    }

    [Fact]
    public void TitleRuleOutput_maps_all_fields()
    {
        var input = new MsgScoring.TitleRuleOutput(
            MsgScoring.TitlePartType.Regex,
            Field: MsgScoring.FilterField.Title,
            Pattern: @"(\d+)",
            CaptureGroup: 1,
            Value: "fallback");

        var result = input.ToApi();

        Assert.Equal(ApiModels.TitlePartType.Regex, result.Type);
        Assert.Equal(ApiModels.FilterField.Title, result.Field);
        Assert.Equal(@"(\d+)", result.Pattern);
        Assert.Equal(1, result.CaptureGroup);
        Assert.Equal("fallback", result.Value);
    }

    [Fact]
    public void EnrichmentConfig_maps_all_sections()
    {
        var config = new EnrichmentConfig(
            true,
            [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
            new TitleMatchConfig(0.85f),
            new AirdateMatchConfig(5, 0.4f),
            new RuntimeMatchConfig(0.25f, RuntimeMode.Filter),
            new YearMatchConfig(2));

        var result = config.ToApi();

        Assert.True(result.Enabled);
        Assert.Equal(2, result.Methods.Length);
        Assert.Equal(ApiModels.EnrichmentMethod.Title, result.Methods[0]);
        Assert.Equal(ApiModels.EnrichmentMethod.Airdate, result.Methods[1]);
        Assert.Equal(0.85f, result.Title.Threshold);
        Assert.Equal(5, result.Airdate.Tolerance);
        Assert.Equal(0.4f, result.Airdate.MinTitleAffinity);
        Assert.Equal(0.25f, result.Runtime.Tolerance);
        Assert.Equal(ApiModels.RuntimeMode.Filter, result.Runtime.Mode);
        Assert.Equal(2, result.Year.Tolerance);
    }

    [Fact]
    public void ScoringHistory_maps_snapshots()
    {
        var requestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var timestamp = new DateTimeOffset(2025, 8, 1, 14, 0, 0, TimeSpan.Zero);
        var snapshot = new ScoringSnapshotSummary(requestId, SearchSource.Sonarr, "tagesschau", timestamp, 25, 3);
        var msg = new ScoringHistoryResult("tagesschau", 1, [snapshot]);

        var result = msg.ToApi();

        Assert.Equal("tagesschau", result.RuleSetId);
        Assert.Equal(1, result.TotalCount);
        var s = Assert.Single(result.Snapshots);
        Assert.Equal(requestId, s.RequestId);
        Assert.Equal(ApiModels.SearchSource.Sonarr, s.Source);
        Assert.Equal("tagesschau", s.Query);
        Assert.Equal(timestamp, s.Timestamp);
        Assert.Equal(25, s.CandidateCount);
        Assert.Equal(3, s.MatchedCount);
    }

    [Fact]
    public void ScoringDetail_maps_item_traces()
    {
        var requestId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var timestamp = new DateTimeOffset(2025, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var candidate = new MsgScoring.ScoreCandidate("Tatort", "Krimi", "ARD", 5400, 1080, "Spannend", 1725100000);
        var trace = new MsgScoring.History.ItemTrace(candidate, true, 0.95, "rule-1", null, []);
        var msg = new ScoringDetailResult(requestId, SearchSource.Radarr, "tatort", timestamp, [trace]);

        var result = msg.ToApi();

        Assert.Equal(requestId, result.RequestId);
        Assert.Equal(ApiModels.SearchSource.Radarr, result.Source);
        Assert.Equal("tatort", result.Query);
        Assert.Equal(timestamp, result.Timestamp);
        var item = Assert.Single(result.ItemTraces);
        Assert.True(item.Matched);
        Assert.Equal(0.95, item.Score);
    }

    [Theory]
    [InlineData("community", ApiModels.SourceType.Community)]
    [InlineData("local", ApiModels.SourceType.Local)]
    [InlineData("merged", ApiModels.SourceType.Merged)]
    public void SourceType_maps_known_strings(string input, ApiModels.SourceType expected)
    {
        var result = input.ToApi();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("something-else")]
    public void SourceType_maps_unknown_to_Unknown(string? input)
    {
        var result = input.ToApi();

        Assert.Equal(ApiModels.SourceType.Unknown, result);
    }

    private static EnrichmentConfig CreateEnrichmentConfig() =>
        new(true,
            [EnrichmentMethod.Title],
            new TitleMatchConfig(0.7f),
            new AirdateMatchConfig(7, 0.3f),
            new RuntimeMatchConfig(0.35f, RuntimeMode.Tiebreaker),
            new YearMatchConfig(1));
}
