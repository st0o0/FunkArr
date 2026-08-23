using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Models;
using FunkArr.RuleSet;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("api/v1/rulesets")]
[Tags("Rulesets")]
public sealed class RulesetController(ActorRegistry actorRegistry) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TestAskTimeout = TimeSpan.FromSeconds(30);

    [HttpGet]
    [ProducesResponseType<RulesetSummaryResponse[]>(200)]
    public async Task<ActionResult<RulesetSummaryResponse[]>> GetAll()
    {
        var registry = await actorRegistry.GetAsync<RuleSetCoordinator>();
        var ledger = await actorRegistry.GetAsync<RuleSetCoordinator>();

        var rulesetsTask = registry.Ask<RuleSetCoordinator.AllRulesetsResponse>(
            new RuleSetCoordinator.GetAllRulesets(), AskTimeout);
        var statsTask = ledger.Ask<MatchQualityWorker.TopicStatsResponse>(
            new MatchQualityWorker.GetAllTopicStats(), AskTimeout);

        await Task.WhenAll(rulesetsTask, statsTask);

        var rulesets = rulesetsTask.Result;
        var stats = statsTask.Result;

        var statsLookup = stats.Stats.ToDictionary(s => s.Topic, StringComparer.OrdinalIgnoreCase);

        var result = rulesets.Rulesets.Select(rs =>
        {
            statsLookup.TryGetValue(rs.Topic, out var topicStats);
            return new RulesetSummaryResponse(
                rs.Topic,
                rs.Source,
                rs.RuleCount,
                rs.Media,
                rs.Aliases,
                topicStats?.MatchRate,
                topicStats?.SearchCount ?? 0);
        }).ToArray();

        return Ok(result);
    }

    [HttpGet("{topic}")]
    [ProducesResponseType<RuleSetFile>(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetOne([FromRoute] string topic)
    {
        var registry = await actorRegistry.GetAsync<RuleSetCoordinator>();
        var response = await registry.Ask<RuleSetCoordinator.RuleSetResponse>(
            new RuleSetCoordinator.GetRuleSet(topic), AskTimeout);

        if (response.RuleSet is null)
        {
            return NotFound();
        }

        return Ok(response.RuleSet);
    }

    [HttpPut("{topic}")]
    [ProducesResponseType<SuccessResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<SuccessResponse>> Save([FromRoute] string topic, [FromBody] RuleSetFile ruleSet)
    {
        var registry = await actorRegistry.GetAsync<RuleSetCoordinator>();

        var response = await registry.Ask<RuleSetCoordinator.SaveLocalRuleSetResponse>(
            new RuleSetCoordinator.SaveLocalRuleSet(ruleSet), AskTimeout);

        return Ok(new SuccessResponse(response.Success));
    }

    [HttpDelete("{topic}")]
    [ProducesResponseType<DeletedResponse>(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete([FromRoute] string topic)
    {
        var registry = await actorRegistry.GetAsync<RuleSetCoordinator>();
        var response = await registry.Ask<RuleSetCoordinator.DeleteLocalRuleSetResponse>(
            new RuleSetCoordinator.DeleteLocalRuleSet(topic), AskTimeout);

        if (!response.Found)
        {
            return NotFound();
        }

        return Ok(new DeletedResponse(true));
    }

    [HttpPost("test")]
    [ProducesResponseType<TestRulesResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<TestRulesResponse>> Test([FromBody] TestRulesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(new ErrorResponse("Invalid test request"));
        }

        var registry = await actorRegistry.GetAsync<RuleSetCoordinator>();

        var response = await registry.Ask<RuleSetCoordinator.TestRulesResponse>(
            new RuleSetCoordinator.TestRules(request.Topic, request.TvdbId, request.Rules ?? []),
            TestAskTimeout);

        return Ok(new TestRulesResponse(response.Matched, response.Filtered, response.Unmatched, response.TotalItems));
    }

    [HttpPost("reload")]
    [ProducesResponseType<ReloadedResponse>(200)]
    public ActionResult<ReloadedResponse> Reload()
    {
        var registry = actorRegistry.Get<RuleSetCoordinator>();
        registry.Tell(new RuleSetCoordinator.ReloadLocal(), ActorRefs.NoSender);
        return Ok(new ReloadedResponse(true));
    }
}
