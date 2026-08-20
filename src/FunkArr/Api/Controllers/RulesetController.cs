using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using Asp.Versioning;
using FunkArr.Api.Models;
using FunkArr.RuleSet;
using Microsoft.AspNetCore.Mvc;

namespace FunkArr.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rulesets")]
[Tags("Rulesets")]
public sealed class RulesetController(ActorRegistry actorRegistry) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TestAskTimeout = TimeSpan.FromSeconds(30);

    [HttpGet]
    [ProducesResponseType<RulesetSummaryResponse[]>(200)]
    public async Task<IActionResult> GetAll()
    {
        var registry = await actorRegistry.GetAsync<RuleSetRegistryActor>();
        var ledger = await actorRegistry.GetAsync<MatchLedgerActor>();

        var rulesetsTask = registry.Ask<RuleSetRegistryActor.AllRulesetsResponse>(
            new RuleSetRegistryActor.GetAllRulesets(), AskTimeout);
        var statsTask = ledger.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetAllTopicStats(), AskTimeout);

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
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetOne(string topic)
    {
        var registry = await actorRegistry.GetAsync<RuleSetRegistryActor>();
        var response = await registry.Ask<RuleSetRegistryActor.RuleSetResponse>(
            new RuleSetRegistryActor.GetRuleSet(topic), AskTimeout);

        if (response.RuleSet is null)
        {
            return NotFound();
        }

        return new JsonResult(response.RuleSet, RuleSetJsonOptions.Default);
    }

    [HttpPut("{topic}")]
    [ProducesResponseType<SuccessResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Save(string topic)
    {
        var registry = await actorRegistry.GetAsync<RuleSetRegistryActor>();

        var ruleSet = await JsonSerializer.DeserializeAsync<RuleSetFile>(
            Request.Body, RuleSetJsonOptions.Default);

        if (ruleSet is null)
        {
            return BadRequest(new ErrorResponse("Invalid ruleset"));
        }

        var response = await registry.Ask<RuleSetRegistryActor.SaveLocalRuleSetResponse>(
            new RuleSetRegistryActor.SaveLocalRuleSet(ruleSet), AskTimeout);

        return Ok(new SuccessResponse(response.Success));
    }

    [HttpDelete("{topic}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string topic)
    {
        var registry = await actorRegistry.GetAsync<RuleSetRegistryActor>();
        var response = await registry.Ask<RuleSetRegistryActor.DeleteLocalRuleSetResponse>(
            new RuleSetRegistryActor.DeleteLocalRuleSet(topic), AskTimeout);

        if (!response.Found)
        {
            return NotFound();
        }

        return Ok(new { deleted = true });
    }

    [HttpPost("test")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Test()
    {
        var registry = await actorRegistry.GetAsync<RuleSetRegistryActor>();

        var request = await JsonSerializer.DeserializeAsync<TestRulesRequest>(
            Request.Body, RuleSetJsonOptions.Default);

        if (request is null || string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(new ErrorResponse("Invalid test request"));
        }

        var response = await registry.Ask<RuleSetRegistryActor.TestRulesResponse>(
            new RuleSetRegistryActor.TestRules(request.Topic, request.TvdbId, request.Rules ?? []),
            TestAskTimeout);

        return Ok(new
        {
            response.Matched,
            response.Filtered,
            response.Unmatched,
            response.TotalItems,
        });
    }

    [HttpPost("reload")]
    [ProducesResponseType(200)]
    public IActionResult Reload()
    {
        var registry = actorRegistry.Get<RuleSetRegistryActor>();
        registry.Tell(new RuleSetRegistryActor.ReloadLocal(), ActorRefs.NoSender);
        return Ok(new { reloaded = true });
    }

    private sealed record TestRulesRequest
    {
        public string Topic { get; init; } = string.Empty;
        public int? TvdbId { get; init; }
        public IReadOnlyList<Rule>? Rules { get; init; }
    }
}
