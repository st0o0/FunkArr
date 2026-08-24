using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Mapping;
using FunkArr.RuleSet;
using Microsoft.AspNetCore.Mvc;
using Contracts = FunkArr.Api.Contracts;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("api/v1/matches")]
[Tags("Match Intelligence")]
public sealed class MatchIntelligenceController(ActorRegistry actorRegistry) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    [HttpGet("recent")]
    [ProducesResponseType<IReadOnlyList<Contracts.MatchSummary>>(200)]
    public async Task<ActionResult<IReadOnlyList<Contracts.MatchSummary>>> GetRecentMatches([FromQuery] int limit = 50)
    {
        var ledger = await actorRegistry.GetAsync<RuleSetActor>();
        var response = await ledger.Ask<MatchQualityActor.RecentMatchesResponse>(
            new MatchQualityActor.GetRecentMatches(limit), AskTimeout);
        return Ok(response.Records.Select(r => r.ToContract()).ToList());
    }

    [HttpGet("topics")]
    [ProducesResponseType<IReadOnlyList<Contracts.TopicSummary>>(200)]
    public async Task<ActionResult<IReadOnlyList<Contracts.TopicSummary>>> GetAllTopicStats()
    {
        var ledger = await actorRegistry.GetAsync<RuleSetActor>();
        var response = await ledger.Ask<MatchQualityActor.TopicStatsResponse>(
            new MatchQualityActor.GetAllTopicStats(), AskTimeout);
        return Ok(response.Stats.Select(s => s.ToContract()).ToList());
    }

    [HttpGet("topics/{topic}")]
    [ProducesResponseType<Contracts.TopicSummary>(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTopicStats([FromRoute] string topic)
    {
        var ledger = await actorRegistry.GetAsync<RuleSetActor>();
        var response = await ledger.Ask<MatchQualityActor.TopicStatsResponse>(
            new MatchQualityActor.GetTopicStats(topic), AskTimeout);

        if (response.Stats.Count == 0)
        {
            return NotFound();
        }

        return Ok(response.Stats[0].ToContract());
    }

    [HttpGet("unmatched")]
    [ProducesResponseType<IReadOnlyList<Contracts.UnmatchedGroup>>(200)]
    public async Task<ActionResult<IReadOnlyList<Contracts.UnmatchedGroup>>> GetUnmatched([FromQuery] string? topic)
    {
        var ledger = await actorRegistry.GetAsync<RuleSetActor>();
        var response = await ledger.Ask<MatchQualityActor.UnmatchedItemsResponse>(
            new MatchQualityActor.GetUnmatchedItems(topic), AskTimeout);
        return Ok(response.Groups.Select(g => g.ToContract()).ToList());
    }
}
