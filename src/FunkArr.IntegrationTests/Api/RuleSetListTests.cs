using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models.RuleSets;
using FunkArr.Core.Actors;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.IntegrationTests.Api;

[Collection("RuleSets")]
public sealed class RuleSetListTests(FunkArrFixture fixture)
{
    [Fact]
    public async Task List_ReturnsRuleSetsWithStatsAndSummaries()
    {
        var task = fixture.Client.GetAsync("/api/rulesets", TestContext.Current.CancellationToken);

        var resolverProbe = fixture.GetProbe<IRuleSetResolver>();
        resolverProbe.ExpectMsg<QueryRegisteredRuleSets>(cancellationToken: TestContext.Current.CancellationToken);
        resolverProbe.Reply(new RegisteredRuleSetsResult(
        [
            new RegisteredRuleSetEntry(
                "tatort", "Tatort", ["tatort-berlin"],
                new ExternalIds(12345, "tt1234567", null),
                "Tatort", Messages.MediaType.Show)
        ]));

        var managerProbe = fixture.GetProbe<IRuleSetManager>();
        managerProbe.ExpectMsg<QueryRuleSetSummaries>(cancellationToken: TestContext.Current.CancellationToken);
        managerProbe.Reply(new RuleSetSummaryResult(
        [
            new RuleSetSummaryEntry("tatort", 3, "community")
        ]));

        var statsProbe = fixture.GetProbe<IStatsCollector>();
        statsProbe.ExpectMsg<QueryAllStats>(cancellationToken: TestContext.Current.CancellationToken);
        statsProbe.Reply(new AllStatsResult(
            ImmutableDictionary<string, ScoringStatsResult>.Empty.Add(
                "tatort", new ScoringStatsResult(DateTimeOffset.UtcNow, 0.75))));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RuleSetListResponse>(cancellationToken: TestContext.Current.CancellationToken);
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
        var task = fixture.Client.GetAsync("/api/rulesets", TestContext.Current.CancellationToken);

        var resolverProbe = fixture.GetProbe<IRuleSetResolver>();
        resolverProbe.ExpectMsg<QueryRegisteredRuleSets>(cancellationToken: TestContext.Current.CancellationToken);
        resolverProbe.Reply(new RegisteredRuleSetsResult([]));

        var managerProbe = fixture.GetProbe<IRuleSetManager>();
        managerProbe.ExpectMsg<QueryRuleSetSummaries>(cancellationToken: TestContext.Current.CancellationToken);
        managerProbe.Reply(new RuleSetSummaryResult([]));

        var statsProbe = fixture.GetProbe<IStatsCollector>();
        statsProbe.ExpectMsg<QueryAllStats>(cancellationToken: TestContext.Current.CancellationToken);
        statsProbe.Reply(new AllStatsResult([]));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RuleSetListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Empty(result.Rulesets);
    }
}
