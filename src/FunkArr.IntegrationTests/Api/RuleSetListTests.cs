using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.IntegrationTests.Api;

[Collection("RuleSets")]
public sealed class RuleSetListTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task List_ReturnsRuleSetsWithStatsAndSummaries()
    {
        var task = _fixture.Client.GetAsync("/api/rulesets");

        var resolverProbe = _fixture.GetProbe<IRuleSetResolver>();
        resolverProbe.ExpectMsg<QueryRegisteredRuleSets>();
        resolverProbe.Reply(new RegisteredRuleSetsResult(
        [
            new RegisteredRuleSetEntry(
                "tatort", "Tatort", ["tatort-berlin"],
                new ExternalIds(12345, "tt1234567", null),
                "Tatort", Messages.MediaType.Show),
        ]));

        var managerProbe = _fixture.GetProbe<IRuleSetManager>();
        managerProbe.ExpectMsg<QueryRuleSetSummaries>();
        managerProbe.Reply(new RuleSetSummaryResult(
        [
            new RuleSetSummaryEntry("tatort", 3, "community"),
        ]));

        var statsProbe = _fixture.GetProbe<IStatsCollector>();
        statsProbe.ExpectMsg<QueryAllStats>();
        statsProbe.Reply(new AllStatsResult(
            ImmutableDictionary<string, ScoringStatsResult>.Empty.Add(
                "tatort", new ScoringStatsResult(DateTimeOffset.UtcNow, 0.75))));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RuleSetListResponse>();
        Assert.NotNull(result);

        var entry = Assert.Single(result.Rulesets);
        Assert.Equal("tatort", entry.RuleSetId);
        Assert.Equal("Tatort", entry.Topic);
        Assert.Equal(3, entry.RuleCount);
        Assert.Equal(SourceType.Community, entry.SourceType);
        Assert.Equal(12345, entry.TvdbId);
        Assert.Equal(0.75, entry.MatchRate);
    }

    [Fact]
    public async Task List_Empty_ReturnsEmptyArray()
    {
        var task = _fixture.Client.GetAsync("/api/rulesets");

        var resolverProbe = _fixture.GetProbe<IRuleSetResolver>();
        resolverProbe.ExpectMsg<QueryRegisteredRuleSets>();
        resolverProbe.Reply(new RegisteredRuleSetsResult([]));

        var managerProbe = _fixture.GetProbe<IRuleSetManager>();
        managerProbe.ExpectMsg<QueryRuleSetSummaries>();
        managerProbe.Reply(new RuleSetSummaryResult([]));

        var statsProbe = _fixture.GetProbe<IStatsCollector>();
        statsProbe.ExpectMsg<QueryAllStats>();
        statsProbe.Reply(new AllStatsResult(ImmutableDictionary<string, ScoringStatsResult>.Empty));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RuleSetListResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Rulesets);
    }
}
