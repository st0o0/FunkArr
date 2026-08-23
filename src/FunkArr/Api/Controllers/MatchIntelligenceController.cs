using Akka.Actor;
using Akka.Hosting;
using FunkArr.RuleSet;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("api/v1/matches")]
[Tags("Match Intelligence")]
public sealed class MatchIntelligenceController(ActorRegistry actorRegistry) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    [HttpGet("recent")]
    [ProducesResponseType<IReadOnlyList<MatchRecord>>(200)]
    public async Task<ActionResult<IReadOnlyList<MatchRecord>>> GetRecentMatches([FromQuery] int limit = 50)
    {
        var ledger = await actorRegistry.GetAsync<RuleSetCoordinator>();
        var response = await ledger.Ask<MatchQualityWorker.RecentMatchesResponse>(
            new MatchQualityWorker.GetRecentMatches(limit), AskTimeout);
        return Ok(response.Records);
    }

    [HttpGet("topics")]
    [ProducesResponseType<IReadOnlyList<TopicStats>>(200)]
    public async Task<ActionResult<IReadOnlyList<TopicStats>>> GetAllTopicStats()
    {
        var ledger = await actorRegistry.GetAsync<RuleSetCoordinator>();
        var response = await ledger.Ask<MatchQualityWorker.TopicStatsResponse>(
            new MatchQualityWorker.GetAllTopicStats(), AskTimeout);
        return Ok(response.Stats);
    }

    [HttpGet("topics/{topic}")]
    [ProducesResponseType<TopicStats>(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTopicStats([FromRoute] string topic)
    {
        var ledger = await actorRegistry.GetAsync<RuleSetCoordinator>();
        var response = await ledger.Ask<MatchQualityWorker.TopicStatsResponse>(
            new MatchQualityWorker.GetTopicStats(topic), AskTimeout);

        if (response.Stats.Count == 0)
        {
            return NotFound();
        }

        return Ok(response.Stats[0]);
    }

    [HttpGet("unmatched")]
    [ProducesResponseType<IReadOnlyList<MatchQualityWorker.UnmatchedGroup>>(200)]
    public async Task<ActionResult<IReadOnlyList<MatchQualityWorker.UnmatchedGroup>>> GetUnmatched([FromQuery] string? topic)
    {
        var ledger = await actorRegistry.GetAsync<RuleSetCoordinator>();
        var response = await ledger.Ask<MatchQualityWorker.UnmatchedItemsResponse>(
            new MatchQualityWorker.GetUnmatchedItems(topic), AskTimeout);
        return Ok(response.Groups);
    }
}
