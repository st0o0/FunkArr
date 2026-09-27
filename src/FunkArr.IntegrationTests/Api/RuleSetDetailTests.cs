using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;
using MsgEnrichment = FunkArr.Messages.Enrichment;

namespace FunkArr.IntegrationTests.Api;

[Collection("RuleSets")]
public sealed class RuleSetDetailTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Detail_ReturnsRuleSetDetail()
    {
        var task = _fixture.Client.GetAsync("/api/rulesets/tatort");

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<QueryRuleSetDetail>();
        probe.Reply(new RuleSetDetailResult(
            "tatort",
            new RuleSetDetailResult.RuleSetIdentity(
                "Tatort", ["tatort-berlin"],
                new ExternalIds(12345, null, null)),
            new RuleSetDetailResult.RuleSetSource(
                "/community/tatort.json", "/local/tatort.json",
                DateTime.UtcNow, DateTime.UtcNow),
            0.8f,
            [
                new Messages.RuleSet.RuleSetDetailRule(
                    "default", 0, null,
                    Messages.Scoring.IdentificationStrategy.SeasonAndEpisodeNumber,
                    @"S(\d+)", @"E(\d+)", 1, null, null),
            ],
            new MsgEnrichment.EnrichmentConfig(
                true,
                [MsgEnrichment.EnrichmentMethod.Title, MsgEnrichment.EnrichmentMethod.Airdate],
                new MsgEnrichment.TitleMatchConfig(0.7f),
                new MsgEnrichment.AirdateMatchConfig(7, 0.3f),
                new MsgEnrichment.RuntimeMatchConfig(0.35f, MsgEnrichment.RuntimeMode.Tiebreaker),
                new MsgEnrichment.YearMatchConfig(1))));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RuleSetDetail>();
        Assert.NotNull(result);
        Assert.Equal("tatort", result.RuleSetId);
        Assert.Equal("Tatort", result.Identity.Topic);
        Assert.Equal(12345, result.Identity.TvdbId);
        Assert.Equal(0.8f, result.DefaultConfidence);

        var rule = Assert.Single(result.Rules);
        Assert.Equal("default", rule.Id);
    }

    [Fact]
    public async Task Detail_NotFound_Returns404()
    {
        var task = _fixture.Client.GetAsync("/api/rulesets/nonexistent");

        var probe = _fixture.GetProbe<IRuleSetRegion>();
        probe.ExpectMsg<QueryRuleSetDetail>();
        probe.Reply(new RuleSetDetailFailed(new Exception("not found")));

        var response = await task;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
