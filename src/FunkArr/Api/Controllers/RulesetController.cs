using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Mapping;
using FunkArr.RuleSet;
using Microsoft.AspNetCore.Mvc;
using Contracts = FunkArr.Api.Contracts;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("api/v1/rulesets")]
[Tags("Rulesets")]
public sealed class RulesetController(ActorRegistry actorRegistry) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TestAskTimeout = TimeSpan.FromSeconds(30);

    [HttpGet]
    [ProducesResponseType<Contracts.RulesetSummary[]>(200)]
    public async Task<ActionResult<Contracts.RulesetSummary[]>> GetAll()
    {
        var registry = await actorRegistry.GetAsync<RuleSetActor>();
        var ledger = await actorRegistry.GetAsync<RuleSetActor>();

        var rulesetsTask = registry.Ask<RuleSetActor.AllRulesetsResponse>(
            new RuleSetActor.GetAllRulesets(), AskTimeout);
        var statsTask = ledger.Ask<MatchQualityActor.TopicStatsResponse>(
            new MatchQualityActor.GetAllTopicStats(), AskTimeout);

        await Task.WhenAll(rulesetsTask, statsTask);

        var rulesets = rulesetsTask.Result;
        var stats = statsTask.Result;

        var statsLookup = stats.Stats.ToDictionary(s => s.Topic, StringComparer.OrdinalIgnoreCase);

        var result = rulesets.Rulesets.Select(rs =>
        {
            statsLookup.TryGetValue(rs.Topic, out var topicStats);
            return rs.ToContract(topicStats);
        }).ToArray();

        return Ok(result);
    }

    [HttpGet("{topic}")]
    [ProducesResponseType<Contracts.RulesetDetail>(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetOne([FromRoute] string topic)
    {
        var registry = await actorRegistry.GetAsync<RuleSetActor>();
        var response = await registry.Ask<RuleSetActor.RuleSetResponse>(
            new RuleSetActor.GetRuleSet(topic), AskTimeout);

        if (response.RuleSet is null)
        {
            return NotFound();
        }

        return Ok(response.RuleSet);
    }

    [HttpPut("{topic}")]
    [ProducesResponseType<Contracts.SuccessResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<Contracts.SuccessResponse>> Save([FromRoute] string topic, [FromBody] RuleSetFile ruleSet)
    {
        var registry = await actorRegistry.GetAsync<RuleSetActor>();

        var response = await registry.Ask<RuleSetActor.SaveLocalRuleSetResponse>(
            new RuleSetActor.SaveLocalRuleSet(ruleSet), AskTimeout);

        return Ok(new Contracts.SuccessResponse(response.Success));
    }

    [HttpDelete("{topic}")]
    [ProducesResponseType<Contracts.DeletedResponse>(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete([FromRoute] string topic)
    {
        var registry = await actorRegistry.GetAsync<RuleSetActor>();
        var response = await registry.Ask<RuleSetActor.DeleteLocalRuleSetResponse>(
            new RuleSetActor.DeleteLocalRuleSet(topic), AskTimeout);

        if (!response.Found)
        {
            return NotFound();
        }

        return Ok(new Contracts.DeletedResponse(true));
    }

    [HttpPost("test")]
    [ProducesResponseType<Contracts.TestRulesResult>(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<Contracts.TestRulesResult>> Test([FromBody] Contracts.TestRulesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(new Contracts.ErrorResponse("Invalid test request"));
        }

        var registry = await actorRegistry.GetAsync<RuleSetActor>();

        var response = await registry.Ask<RuleSetActor.TestRulesResponse>(
            new RuleSetActor.TestRules(request.Topic, (int?)request.TvdbId, []),
            TestAskTimeout);

        return Ok(response.ToContract());
    }

    [HttpPost("reload")]
    [ProducesResponseType<Contracts.ReloadedResponse>(200)]
    public ActionResult<Contracts.ReloadedResponse> Reload()
    {
        var registry = actorRegistry.Get<RuleSetActor>();
        registry.Tell(new RuleSetActor.ReloadLocal(), ActorRefs.NoSender);
        return Ok(new Contracts.ReloadedResponse(true));
    }
}
