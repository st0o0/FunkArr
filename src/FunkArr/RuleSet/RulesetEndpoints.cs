using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;

namespace FunkArr.RuleSet;

public static class RulesetEndpoints
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TestAskTimeout = TimeSpan.FromSeconds(30);

    public static void MapRulesetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rulesets")
            .AddEndpointFilter<MatchApiKeyFilter>();

        group.MapGet("/", HandleGetAll);
        group.MapGet("/{topic}", HandleGetOne);
        group.MapPut("/{topic}", HandleSave);
        group.MapDelete("/{topic}", HandleDelete);
        group.MapPost("/test", HandleTest);
        group.MapPost("/reload", HandleReload);
    }

    private static async Task<IResult> HandleGetAll(ActorRegistry actorRegistry)
    {
        var registry = actorRegistry.Get<RuleSetRegistryActor>();
        var ledger = actorRegistry.Get<MatchLedgerActor>();

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
            return new
            {
                rs.Topic,
                rs.Source,
                rs.RuleCount,
                rs.Media,
                rs.Aliases,
                MatchRate = topicStats?.MatchRate,
                SearchCount = topicStats?.SearchCount ?? 0,
            };
        }).ToList();

        return Results.Json(result);
    }

    private static async Task<IResult> HandleGetOne(string topic, ActorRegistry actorRegistry)
    {
        var registry = actorRegistry.Get<RuleSetRegistryActor>();
        var response = await registry.Ask<RuleSetRegistryActor.RuleSetResponse>(
            new RuleSetRegistryActor.GetRuleSet(topic), AskTimeout);

        if (response.RuleSet is null)
        {
            return Results.NotFound();
        }

        return Results.Json(response.RuleSet, RuleSetJsonOptions.Default);
    }

    private static async Task<IResult> HandleSave(
        string topic,
        HttpContext context,
        ActorRegistry actorRegistry)
    {
        var registry = actorRegistry.Get<RuleSetRegistryActor>();

        var ruleSet = await JsonSerializer.DeserializeAsync<RuleSetFile>(
            context.Request.Body, RuleSetJsonOptions.Default);

        if (ruleSet is null)
        {
            return Results.BadRequest(new { error = "Invalid ruleset" });
        }

        var response = await registry.Ask<RuleSetRegistryActor.SaveLocalRuleSetResponse>(
            new RuleSetRegistryActor.SaveLocalRuleSet(ruleSet), AskTimeout);

        return Results.Json(new { success = response.Success });
    }

    private static async Task<IResult> HandleDelete(string topic, ActorRegistry actorRegistry)
    {
        var registry = actorRegistry.Get<RuleSetRegistryActor>();
        var response = await registry.Ask<RuleSetRegistryActor.DeleteLocalRuleSetResponse>(
            new RuleSetRegistryActor.DeleteLocalRuleSet(topic), AskTimeout);

        if (!response.Found)
        {
            return Results.NotFound();
        }

        return Results.Json(new { deleted = true });
    }

    private static async Task<IResult> HandleTest(HttpContext context, ActorRegistry actorRegistry)
    {
        var registry = actorRegistry.Get<RuleSetRegistryActor>();

        var request = await JsonSerializer.DeserializeAsync<TestRulesRequest>(
            context.Request.Body, RuleSetJsonOptions.Default);

        if (request is null || string.IsNullOrWhiteSpace(request.Topic))
        {
            return Results.BadRequest(new { error = "Invalid test request" });
        }

        var response = await registry.Ask<RuleSetRegistryActor.TestRulesResponse>(
            new RuleSetRegistryActor.TestRules(request.Topic, request.TvdbId, request.Rules ?? []),
            TestAskTimeout);

        return Results.Json(new
        {
            response.Matched,
            response.Filtered,
            response.Unmatched,
            response.TotalItems,
        });
    }

    private static IResult HandleReload(ActorRegistry actorRegistry)
    {
        var registry = actorRegistry.Get<RuleSetRegistryActor>();
        registry.Tell(new RuleSetRegistryActor.ReloadLocal());
        return Results.Ok(new { reloaded = true });
    }

    private sealed record TestRulesRequest
    {
        public string Topic { get; init; } = string.Empty;
        public int? TvdbId { get; init; }
        public IReadOnlyList<Rule>? Rules { get; init; }
    }
}
