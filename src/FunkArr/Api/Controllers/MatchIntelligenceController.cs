using Akka.Actor;
using Akka.Hosting;
using Asp.Versioning;
using FunkArr.RuleSet;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/matches")]
[Tags("Match Intelligence")]
public sealed class MatchIntelligenceController(ActorRegistry actorRegistry) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    [HttpGet("recent")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetRecentMatches([FromQuery] int limit = 50)
    {
        var ledger = await actorRegistry.GetAsync<MatchLedgerActor>();
        var response = await ledger.Ask<MatchLedgerActor.RecentMatchesResponse>(
            new MatchLedgerActor.GetRecentMatches(limit), AskTimeout);
        return Ok(response.Records);
    }

    [HttpGet("topics")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAllTopicStats()
    {
        var ledger = await actorRegistry.GetAsync<MatchLedgerActor>();
        var response = await ledger.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetAllTopicStats(), AskTimeout);
        return Ok(response.Stats);
    }

    [HttpGet("topics/{topic}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTopicStats(string topic)
    {
        var ledger = await actorRegistry.GetAsync<MatchLedgerActor>();
        var response = await ledger.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetTopicStats(topic), AskTimeout);

        if (response.Stats.Count == 0)
        {
            return NotFound();
        }

        return Ok(response.Stats[0]);
    }

    [HttpGet("unmatched")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetUnmatched([FromQuery] string? topic)
    {
        var ledger = await actorRegistry.GetAsync<MatchLedgerActor>();
        var response = await ledger.Ask<MatchLedgerActor.UnmatchedItemsResponse>(
            new MatchLedgerActor.GetUnmatchedItems(topic), AskTimeout);
        return Ok(response.Groups);
    }
}
