using Akka.Actor;
using Akka.Hosting;
using FunkArr.Configuration;
using Microsoft.Extensions.Options;

namespace FunkArr.RuleSet;

public static class MatchIntelligenceEndpoints
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    public static void MapMatchIntelligenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matches")
            .AddEndpointFilter<MatchApiKeyFilter>();

        group.MapGet("/recent", HandleRecentMatches);
        group.MapGet("/topics", HandleAllTopicStats);
        group.MapGet("/topics/{topic}", HandleTopicStats);
        group.MapGet("/unmatched", HandleUnmatched);
    }

    private static async Task<IResult> HandleRecentMatches(
        HttpContext context,
        ActorRegistry actorRegistry)
    {
        var limit = int.TryParse(context.Request.Query["limit"], out var l) ? l : 50;
        var ledger = actorRegistry.Get<MatchLedgerActor>();
        var response = await ledger.Ask<MatchLedgerActor.RecentMatchesResponse>(
            new MatchLedgerActor.GetRecentMatches(limit), AskTimeout);
        return Results.Ok(response.Records);
    }

    private static async Task<IResult> HandleAllTopicStats(
        ActorRegistry actorRegistry)
    {
        var ledger = actorRegistry.Get<MatchLedgerActor>();
        var response = await ledger.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetAllTopicStats(), AskTimeout);
        return Results.Ok(response.Stats);
    }

    private static async Task<IResult> HandleTopicStats(
        string topic,
        ActorRegistry actorRegistry)
    {
        var ledger = actorRegistry.Get<MatchLedgerActor>();
        var response = await ledger.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetTopicStats(topic), AskTimeout);

        if (response.Stats.Count == 0)
        {
            return Results.NotFound();
        }

        return Results.Ok(response.Stats[0]);
    }

    private static async Task<IResult> HandleUnmatched(
        HttpContext context,
        ActorRegistry actorRegistry)
    {
        var topic = context.Request.Query["topic"].FirstOrDefault();
        var ledger = actorRegistry.Get<MatchLedgerActor>();
        var response = await ledger.Ask<MatchLedgerActor.UnmatchedItemsResponse>(
            new MatchLedgerActor.GetUnmatchedItems(topic), AskTimeout);
        return Results.Ok(response.Groups);
    }
}

public sealed class MatchApiKeyFilter(IOptions<FunkArrOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var apiKey = context.HttpContext.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || apiKey != options.Value.ApiKey)
        {
            return Results.Json(new { error = "Incorrect user credentials" }, statusCode: 401);
        }

        return await next(context);
    }
}
