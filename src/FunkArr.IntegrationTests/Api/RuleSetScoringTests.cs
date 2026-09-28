using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Scoring;
using MsgHistory = FunkArr.Messages.Scoring.History;

namespace FunkArr.IntegrationTests.Api;

[Collection("RuleSets")]
public sealed class RuleSetScoringTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task TestScoring_ReturnsItemTraces()
    {
        var request = new TestScoreRequest(
            DefaultConfidence: 0.8f,
            Rules:
            [
                new RuleInput("rule-1",
                    Strategy: FunkArr.Api.Models.IdentificationStrategy.SeasonAndEpisodeNumber,
                    SeasonRegex: @"S(\d+)",
                    EpisodeRegex: @"E(\d+)")
            ],
            Candidates:
            [
                new TestCandidate(
                    Title: "Tatort S01E05 Der letzte Fall",
                    Topic: "Tatort",
                    Channel: "ARD",
                    Duration: 5400)
            ]);

        var task = _fixture.Client.PostAsJsonAsync("/api/rulesets/test", request);

        var probe = _fixture.GetProbe<IScoringManager>();
        var msg = probe.ExpectMsg<TestScoreItems>();
        probe.Reply(new TestScoreCompleted(
            msg.RequestId,
            [
                new MsgHistory.ItemTrace(
                    new Messages.Scoring.ScoreCandidate("Tatort S01E05 Der letzte Fall", "Tatort", "ARD", 5400, 0, null, 0),
                    Matched: true,
                    Score: 0.8,
                    MatchedRuleId: "rule-1",
                    Identification: new MsgHistory.TracedIdentification("01", "05", null),
                    RuleTraces:
                    [
                        new MsgHistory.RuleTrace("rule-1", 0, MsgHistory.RuleOutcome.Matched, null, null)
                    ])
            ]));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TestScoreResponse>();
        Assert.NotNull(result);

        var trace = Assert.Single(result.ItemTraces);
        Assert.True(trace.Matched);
        Assert.Equal("rule-1", trace.MatchedRuleId);
        Assert.Equal(0.8, trace.Score);
    }
}
