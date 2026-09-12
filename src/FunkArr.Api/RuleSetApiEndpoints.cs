using System.Text.Json;
using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static partial class RuleSetApiEndpoints
{
    private static readonly TimeSpan _queryTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _statsTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(15);
    private static readonly Regex _ruleSetIdPattern = RuleSetIdRegex();

    public static WebApplication MapRuleSetApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/rulesets").WithTags("Rulesets");

        group.MapGet("/", async (IActorRegistry registry) =>
        {
            try
            {
                var resolver = await registry.GetAsync<IRuleSetResolver>();
                var manager = await registry.GetAsync<IRuleSetManager>();
                var historyRegion = await registry.GetAsync<IMatchHistoryRegion>();

                var resolverTask = resolver.Ask<RegisteredRuleSetsResult>(
                    new QueryRegisteredRuleSets(), _queryTimeout);
                var summaryTask = manager.Ask<RuleSetSummaryResult>(
                    new QueryRuleSetSummaries(), _queryTimeout);

                await Task.WhenAll(resolverTask, summaryTask);

                var entries = resolverTask.Result;
                var summaries = summaryTask.Result;
                var summaryMap = summaries.Entries.ToDictionary(s => s.RuleSetId);

                var statsTasks = entries.Entries.Select(async e =>
                {
                    try
                    {
                        return await historyRegion.Ask<ScoringStatsResult>(
                            new QueryScoringStats(e.RuleSetId), _statsTimeout);
                    }
                    catch
                    {
                        return new ScoringStatsResult(null, null);
                    }
                }).ToArray();

                var stats = await Task.WhenAll(statsTasks);

                var result = entries.Entries.Select((e, i) =>
                {
                    summaryMap.TryGetValue(e.RuleSetId, out var summary);
                    var stat = stats[i];

                    return new ApiModels.RuleSetListEntry(
                        e.RuleSetId, e.Topic, e.Aliases, e.TvdbId, e.ImdbId, e.TmdbId,
                        e.MediaName,
                        summary?.RuleCount ?? 0,
                        summary?.SourceType ?? "unknown",
                        stat.LastRun?.ToString("o"),
                        stat.MatchRate);
                }).ToArray();

                return Results.Ok(result);
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        })
        .CacheOutput("RuleSetList")
        .WithSummary("List all rulesets")
        .WithDescription("Returns all registered rulesets with rule counts, source type, and scoring stats.")
        .Produces<ApiModels.RuleSetListEntry[]>()
        .ProducesProblem(504);

        group.MapGet("/{id}", async (string id, IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IRuleSetManager>();
            try
            {
                var result = await manager.Ask<IRuleSetResponse>(
                    new QueryRuleSetDetail(id), _queryTimeout);
                return result switch
                {
                    RuleSetDetailResult detail => Results.Ok(ToDetailModel(detail)),
                    RuleSetNotFound => Results.NotFound(),
                    _ => GatewayTimeout(),
                };
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        })
        .WithSummary("Get ruleset details")
        .WithDescription("Returns full ruleset configuration including identity, source info, and all matching rules.")
        .Produces<ApiModels.RuleSetDetail>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapGet("/{id}/history", async (string id, int? offset, int? limit, IActorRegistry registry) =>
        {
            var historyRegion = await registry.GetAsync<IMatchHistoryRegion>();
            try
            {
                var result = await historyRegion.Ask<ScoringHistoryResult>(
                    new QueryScoringHistory(id, offset ?? 0, limit ?? 20), _queryTimeout);
                return Results.Ok(ToHistoryModel(result));
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        })
        .WithSummary("Get scoring history")
        .WithDescription("Returns paginated scoring history for a ruleset.")
        .Produces<ApiModels.ScoringHistory>()
        .ProducesProblem(504);

        group.MapGet("/{id}/history/{requestId:guid}", async (string id, Guid requestId, IActorRegistry registry) =>
        {
            var historyRegion = await registry.GetAsync<IMatchHistoryRegion>();
            try
            {
                var result = await historyRegion.Ask<IScoringResponse>(
                    new QueryScoringDetail(id, requestId), _queryTimeout);
                return result switch
                {
                    ScoringDetailResult detail => Results.Ok(ToScoringDetailModel(detail)),
                    ScoringDetailNotFound => Results.NotFound(),
                    _ => GatewayTimeout(),
                };
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        })
        .WithSummary("Get scoring detail")
        .WithDescription("Returns detailed scoring trace for a specific scoring request, including per-item and per-rule traces.")
        .Produces<ApiModels.ScoringDetail>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapPost("/", HandleCreate)
            .WithSummary("Create local ruleset");
        group.MapPut("/{id}", HandleUpdate)
            .WithSummary("Update ruleset");
        group.MapDelete("/{id}", HandleDelete)
            .WithSummary("Delete local ruleset");
        group.MapGet("/{id}/raw", HandleGetRaw)
            .WithSummary("Get raw ruleset JSON");

        group.MapPost("/test", async (JsonElement body, IActorRegistry registry) =>
        {
            var request = RuleSetTestRequestParser.Parse(body);
            if (request is null)
            {
                return Results.BadRequest(new { error = "Invalid request body" });
            }

            var (config, candidates) = request.Value;

            var manager = await registry.GetAsync<IMatchMagicManager>();
            try
            {
                var result = await manager.Ask<IScoringResponse>(
                    new TestScoreItems(Guid.NewGuid(), config, candidates), _testTimeout);

                return result switch
                {
                    TestScoreCompleted completed => Results.Ok(new
                    {
                        itemTraces = completed.ItemTraces
                            .Select(ToItemTraceModel).ToArray(),
                    }),
                    _ => Results.Problem(statusCode: 504, title: "Gateway Timeout"),
                };
            }
            catch (Exception)
            {
                return Results.Problem(statusCode: 504, title: "Gateway Timeout");
            }
        })
        .WithSummary("Test ruleset scoring")
        .WithDescription("Runs scoring against provided candidates using an ad-hoc ruleset configuration. Returns per-item traces.")
        .Produces<object>()
        .ProducesProblem(400)
        .ProducesProblem(504);

        return app;
    }

    private static IResult GatewayTimeout() =>
        Results.Problem(statusCode: 504, title: "Gateway Timeout");

    private static async Task EvictRuleSetCache(IOutputCacheStore cache) =>
        await cache.EvictByTagAsync("rulesets", default);

    private static async Task<IResult> HandleCreate(JsonElement body, IDataFiles dataFiles, DataPaths dataPaths, IOutputCacheStore cache)
    {
        if (!body.TryGetProperty("ruleSetId", out var idEl) || idEl.ValueKind != JsonValueKind.String)
        {
            return Results.BadRequest(new { error = "ruleSetId is required" });
        }

        var ruleSetId = idEl.GetString()!;
        if (!_ruleSetIdPattern.IsMatch(ruleSetId))
        {
            return Results.BadRequest(new { error = "ruleSetId must be kebab-case (lowercase letters, numbers, hyphens)" });
        }

        if (!body.TryGetProperty("topic", out var topicEl) || topicEl.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(topicEl.GetString()))
        {
            return Results.BadRequest(new { error = "topic is required" });
        }

        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{ruleSetId}.json");
        if (dataFiles.Exists(localPath))
        {
            return Results.Conflict(new { error = $"Local ruleset '{ruleSetId}' already exists" });
        }

        var json = body.GetRawText();
        dataFiles.CreateDirectory(dataPaths.LocalRuleSets);
        dataFiles.WriteAtomic(localPath, json);

        await EvictRuleSetCache(cache);
        return Results.Created($"/api/rulesets/{ruleSetId}", new { ruleSetId });
    }

    private static async Task<IResult> HandleUpdate(string id, JsonElement body, IDataFiles dataFiles, DataPaths dataPaths, IOutputCacheStore cache)
    {
        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{id}.json");
        var communityPath = Path.Join(dataPaths.CommunityRuleSets, $"{id}.json");

        if (!dataFiles.Exists(localPath) && !dataFiles.Exists(communityPath))
        {
            return Results.NotFound();
        }

        var json = body.GetRawText();
        dataFiles.CreateDirectory(dataPaths.LocalRuleSets);
        dataFiles.WriteAtomic(localPath, json);

        await EvictRuleSetCache(cache);
        return Results.Ok();
    }

    private static IResult HandleGetRaw(string id, IDataFiles dataFiles, DataPaths dataPaths)
    {
        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{id}.json");
        var communityPath = Path.Join(dataPaths.CommunityRuleSets, $"{id}.json");

        if (dataFiles.Exists(localPath))
        {
            var json = dataFiles.ReadText(localPath);
            return Results.Content(json, "application/json");
        }

        if (dataFiles.Exists(communityPath))
        {
            var json = dataFiles.ReadText(communityPath);
            return Results.Content(json, "application/json");
        }

        return Results.NotFound();
    }

    private static async Task<IResult> HandleDelete(string id, IDataFiles dataFiles, DataPaths dataPaths, IOutputCacheStore cache)
    {
        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{id}.json");

        if (!dataFiles.Exists(localPath))
        {
            return Results.NotFound();
        }

        dataFiles.Remove(localPath);

        await EvictRuleSetCache(cache);
        return Results.Ok();
    }

    private static ApiModels.RuleSetDetail ToDetailModel(RuleSetDetailResult msg) =>
        new(msg.RuleSetId,
            new ApiModels.RuleSetDetail.RuleSetIdentity(
                msg.Identity.Topic, msg.Identity.Aliases,
                msg.Identity.TvdbId, msg.Identity.ImdbId, msg.Identity.TmdbId),
            new ApiModels.RuleSetDetail.RuleSetSource(
                msg.Source.CommunityPath, msg.Source.LocalPath,
                msg.Source.CommunityModified, msg.Source.LocalModified),
            msg.DefaultConfidence,
            msg.Rules.Select(r => new ApiModels.RuleSetDetailRule(
                r.Id, r.Priority, r.Confidence, r.Strategy,
                r.FilterSummary, r.SeasonPattern, r.EpisodePattern,
                r.MatchMode, r.TitleParts)).ToArray());

    private static ApiModels.ScoringHistory ToHistoryModel(ScoringHistoryResult msg) =>
        new(msg.RuleSetId, msg.TotalCount,
            msg.Snapshots.Select(s => new ApiModels.ScoringSnapshotSummary(
                s.RequestId, s.Source, s.Query, s.Timestamp,
                s.CandidateCount, s.MatchedCount)).ToArray());

    private static ApiModels.ScoringDetail ToScoringDetailModel(ScoringDetailResult msg) =>
        new(msg.RequestId, msg.Source, msg.Query, msg.Timestamp,
            msg.ItemTraces.Select(ToItemTraceModel).ToArray());

    internal static ApiModels.ItemTrace ToItemTraceModel(ItemTrace msg) =>
        new(msg.CandidateTitle, msg.CandidateTopic, msg.CandidateChannel,
            msg.CandidateDuration, msg.CandidateQuality, msg.CandidateDescription,
            msg.CandidateTimestamp, msg.Matched, msg.Score, msg.MatchedRuleId,
            msg.Identification is not null
                ? new ApiModels.TracedIdentification(msg.Identification.Season, msg.Identification.Episode, msg.Identification.Title)
                : null,
            msg.RuleTraces.Select(ToRuleTraceModel).ToArray());

    internal static ApiModels.RuleTrace ToRuleTraceModel(RuleTrace msg) =>
        new(msg.RuleId, msg.Priority, (ApiModels.RuleOutcome)msg.Outcome,
            msg.FilterTrace is not null ? ToFilterGroupModel(msg.FilterTrace) : null,
            msg.IdentificationTrace is not null
                ? new ApiModels.IdentificationTrace(msg.IdentificationTrace.Strategy, msg.IdentificationTrace.Attempted, msg.IdentificationTrace.Detail)
                : null);

    internal static ApiModels.FilterGroupTrace ToFilterGroupModel(FilterGroupTrace msg) =>
        new(msg.Operator, msg.Passed,
            msg.Nodes.Select(n => new ApiModels.FilterNodeTrace(
                n.Field, n.Op, n.ExpectedValue, n.ActualValue, n.Passed, n.Skipped,
                n.Group is not null ? ToFilterGroupModel(n.Group) : null)).ToArray());

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex RuleSetIdRegex();
}
