using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.History;
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

    private static readonly JsonSerializerOptions _diskJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static WebApplication MapRuleSetApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/rulesets")
            .WithTags("Rulesets")
            .AddEndpointFilter<EndpointExceptionFilter>();

        group.MapGet("/", async (IActorRegistry registry, IDataFiles dataFiles, DataPaths dataPaths) =>
        {
            var resolver = await registry.GetAsync<IRuleSetResolver>();
            var manager = await registry.GetAsync<IRuleSetManager>();
            var statsCollector = await registry.GetAsync<IStatsCollector>();

            var resolverTask = resolver.Ask<RegisteredRuleSetsResult>(
                new QueryRegisteredRuleSets(), _queryTimeout);
            var summaryTask = manager.Ask<RuleSetSummaryResult>(
                new QueryRuleSetSummaries(), _queryTimeout);
            var statsTask = statsCollector.Ask<AllStatsSnapshot>(
                new QueryAllStats(), _statsTimeout);

            await Task.WhenAll(resolverTask, summaryTask, statsTask);

            var entries = resolverTask.Result;
            var summaries = summaryTask.Result;
            var summaryMap = summaries.Entries.ToDictionary(s => s.RuleSetId);
            var allStats = statsTask.Result.Entries;

            var result = entries.Entries.Select(e =>
            {
                summaryMap.TryGetValue(e.RuleSetId, out var summary);
                allStats.TryGetValue(e.RuleSetId, out var stat);

                return new ApiModels.RuleSetListEntry(
                    e.RuleSetId, e.Topic, e.Aliases, e.TvdbId, e.ImdbId, e.TmdbId,
                    e.MediaName, e.MediaType,
                    summary?.RuleCount ?? 0,
                    (summary?.SourceType).ToApi(),
                    stat?.LastRun,
                    stat?.MatchRate,
                    stat?.EnrichmentRate);
            }).ToArray();

            var communityVersion = dataFiles.Exists(dataPaths.RuleSetVersion)
                ? dataFiles.ReadText(dataPaths.RuleSetVersion).Trim()
                : null;

            return Results.Ok(new ApiModels.RuleSetListResponse(communityVersion, result));
        })
        .CacheOutput("RuleSetList")
        .WithSummary("List all rulesets")
        .WithDescription("Returns all registered rulesets with community version and rule counts, source type, and scoring stats.")
        .Produces<ApiModels.RuleSetListResponse>()
        .ProducesProblem(504);

        group.MapGet("/{id}", async (string id, IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IRuleSetManager>();
            var result = await manager.Ask<RuleSetDetailResponse>(
                new QueryRuleSetDetail(id), _queryTimeout);
            return result switch
            {
                RuleSetDetailResult detail => Results.Ok(detail.ToApi()),
                RuleSetDetailFailed => Results.NotFound(),
                _ => ApiResults.GatewayTimeout(),
            };
        })
        .WithSummary("Get ruleset details")
        .WithDescription("Returns full ruleset configuration including identity, source info, and all matching rules.")
        .Produces<ApiModels.RuleSetDetail>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapGet("/{id}/history", async (string id, int? offset, int? limit, IActorRegistry registry) =>
        {
            var historyRegion = await registry.GetAsync<IHistoryRegion>();
            var result = await historyRegion.Ask<ScoringHistoryResult>(
                new QueryScoringHistory(id, offset ?? 0, limit ?? 20), _queryTimeout);
            return Results.Ok(result.ToApi());
        })
        .WithSummary("Get scoring history")
        .WithDescription("Returns paginated scoring history for a ruleset.")
        .Produces<ApiModels.ScoringHistory>()
        .ProducesProblem(504);

        group.MapGet("/{id}/history/{requestId:guid}", async (string id, Guid requestId, IActorRegistry registry) =>
        {
            var historyRegion = await registry.GetAsync<IHistoryRegion>();
            var result = await historyRegion.Ask<ScoringDetailResponse>(
                new QueryScoringDetail(id, requestId), _queryTimeout);
            return result switch
            {
                ScoringDetailResult detail => Results.Ok(detail.ToApi()),
                ScoringDetailFailed => Results.NotFound(),
                _ => ApiResults.GatewayTimeout(),
            };
        })
        .WithSummary("Get scoring detail")
        .WithDescription("Returns detailed scoring trace for a specific scoring request, including per-item and per-rule traces.")
        .Produces<ApiModels.ScoringDetail>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapPost("/", HandleCreate)
            .WithSummary("Create local ruleset")
            .Produces<ApiModels.CreatedRuleSetResponse>(201)
            .ProducesProblem(400)
            .ProducesProblem(409)
            .ProducesProblem(422);
        group.MapPut("/{id}", HandleUpdate)
            .WithSummary("Update ruleset")
            .Produces(200)
            .ProducesProblem(404)
            .ProducesProblem(422);
        group.MapDelete("/{id}", HandleDelete)
            .WithSummary("Delete local ruleset")
            .Produces(200)
            .ProducesProblem(404);
        group.MapGet("/{id}/raw", HandleGetRaw)
            .WithSummary("Get raw ruleset JSON");

        group.MapGet("/{id}/export", HandleExport)
            .WithSummary("Export ruleset for community contribution");

        group.MapPost("/test", async (ApiModels.TestScoreRequest request, IActorRegistry registry) =>
        {
            var (config, candidates) = request.ToMessage();

            var manager = await registry.GetAsync<IScoringManager>();
            var result = await manager.Ask<TestScoreItemsResponse>(
                new TestScoreItems(Guid.NewGuid(), config, candidates), _testTimeout);

            if (result is not TestScoreCompleted completed)
            {
                return ApiResults.GatewayTimeout();
            }

            var itemTraces = completed.ItemTraces;
            if (request.Enrichment is not null && request.Enrichment.Enabled != false)
            {
                itemTraces = await RunTestEnrichment(itemTraces, request, registry);
            }

            return Results.Ok(new ApiModels.TestScoreResponse(
                itemTraces.Select(t => t.ToApi()).ToArray()));
        })
        .WithSummary("Test ruleset scoring")
        .WithDescription("Runs scoring against provided candidates using an ad-hoc ruleset configuration with optional enrichment. Returns per-item traces.")
        .Produces<ApiModels.TestScoreResponse>()
        .ProducesProblem(400)
        .ProducesProblem(504);

        return app;
    }

    private static async Task EvictRuleSetCache(IOutputCacheStore cache) =>
        await cache.EvictByTagAsync("rulesets", default);

    private static async Task<IResult> HandleCreate(ApiModels.CreateRuleSetRequest request, IDataFiles dataFiles, DataPaths dataPaths, IOutputCacheStore cache, IRuleSetValidator validator)
    {
        if (string.IsNullOrWhiteSpace(request.RuleSetId))
        {
            return Results.BadRequest(new ApiModels.ErrorResponse("ruleSetId is required"));
        }

        if (!_ruleSetIdPattern.IsMatch(request.RuleSetId))
        {
            return Results.BadRequest(new ApiModels.ErrorResponse("ruleSetId must be kebab-case (lowercase letters, numbers, hyphens)"));
        }

        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{request.RuleSetId}.json");
        if (dataFiles.Exists(localPath))
        {
            return Results.Conflict(new ApiModels.ErrorResponse($"Local ruleset '{request.RuleSetId}' already exists"));
        }

        var json = SerializeForDisk(request);
        var validationErrors = validator.Validate(json);
        if (validationErrors.Count > 0)
        {
            return Results.UnprocessableEntity(new ApiModels.ValidationErrorResponse(validationErrors));
        }

        dataFiles.CreateDirectory(dataPaths.LocalRuleSets);
        dataFiles.WriteAtomic(localPath, json);

        await EvictRuleSetCache(cache);
        return Results.Created($"/api/rulesets/{request.RuleSetId}", new ApiModels.CreatedRuleSetResponse(request.RuleSetId));
    }

    private static async Task<IResult> HandleUpdate(string id, ApiModels.UpdateRuleSetRequest request, IDataFiles dataFiles, DataPaths dataPaths, IOutputCacheStore cache, IRuleSetValidator validator)
    {
        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{id}.json");
        var communityPath = Path.Join(dataPaths.CommunityRuleSets, $"{id}.json");

        if (!dataFiles.Exists(localPath) && !dataFiles.Exists(communityPath))
        {
            return Results.NotFound();
        }

        var json = SerializeForDisk(request);
        var validationErrors = validator.Validate(json);
        if (validationErrors.Count > 0)
        {
            return Results.UnprocessableEntity(new ApiModels.ValidationErrorResponse(validationErrors));
        }

        dataFiles.CreateDirectory(dataPaths.LocalRuleSets);
        dataFiles.WriteAtomic(localPath, json);

        await EvictRuleSetCache(cache);
        return Results.Ok();
    }

    private static string SerializeForDisk(ApiModels.CreateRuleSetRequest request) =>
        JsonSerializer.Serialize(new
        {
            request.Topic,
            request.Aliases,
            request.Media,
            request.Confidence,
            request.Rules,
            request.Standalone,
            request.Disable,
            request.Enrichment,
        }, _diskJsonOptions);

    private static string SerializeForDisk(ApiModels.UpdateRuleSetRequest request) =>
        JsonSerializer.Serialize(new
        {
            request.Topic,
            request.Aliases,
            request.Media,
            request.Confidence,
            request.Rules,
            request.Standalone,
            request.Disable,
            request.Enrichment,
        }, _diskJsonOptions);

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

    private static IResult HandleExport(string id, IRuleSetExporter exporter, HttpContext httpContext)
    {
        var result = exporter.Export(id);

        if (result.Error is not null)
        {
            return Results.NotFound(new ApiModels.ErrorResponse(result.Error));
        }

        if (result.Errors is { Count: > 0 })
        {
            return Results.UnprocessableEntity(new ApiModels.ValidationErrorResponse(result.Errors));
        }

        httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{id}.json\"";
        return Results.Content(result.Json!, "application/json");
    }

    private static readonly TimeSpan _enrichmentTimeout = TimeSpan.FromSeconds(10);

    private static async Task<ItemTrace[]> RunTestEnrichment(
        ItemTrace[] itemTraces, ApiModels.TestScoreRequest request, IActorRegistry registry)
    {
        var enrichmentConfig = request.Enrichment!.ToMessage();
        var isShow = string.Equals(request.MediaType, "show", StringComparison.OrdinalIgnoreCase);
        var isMovie = string.Equals(request.MediaType, "movie", StringComparison.OrdinalIgnoreCase);

        try
        {
            var enrichmentManager = await registry.GetAsync<IEnrichmentManager>();

            if (isShow && request.TvdbId is not null)
            {
                return await EnrichEpisodes(itemTraces, request, enrichmentConfig, enrichmentManager);
            }

            if (isMovie && (request.TmdbId is not null || request.ImdbId is not null))
            {
                return await EnrichMovies(itemTraces, request, enrichmentConfig, enrichmentManager);
            }
        }
        catch
        {
            // enrichment timeout or failure — return scoring results without enrichment
        }

        return itemTraces;
    }

    private static async Task<ItemTrace[]> EnrichEpisodes(
        ItemTrace[] itemTraces, ApiModels.TestScoreRequest request,
        EnrichmentConfig config, IActorRef enrichmentManager)
    {
        var matchedIndices = new List<(int TraceIndex, EpisodeCandidate Candidate)>();

        for (var i = 0; i < itemTraces.Length; i++)
        {
            var trace = itemTraces[i];
            if (!trace.Matched)
            {
                continue;
            }

            var airedAt = trace.CandidateTimestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(trace.CandidateTimestamp)
                : (DateTimeOffset?)null;

            int? season = null;
            if (trace.Identification?.Season is not null && int.TryParse(trace.Identification.Season, out var s))
            {
                season = s;
            }

            matchedIndices.Add((i, new EpisodeCandidate(
                matchedIndices.Count,
                trace.CandidateTitle,
                trace.Identification?.Title,
                airedAt,
                trace.CandidateDuration,
                trace.Identification?.Season,
                trace.Identification?.Episode)));
        }

        if (matchedIndices.Count == 0)
        {
            return itemTraces;
        }

        var enrichRequest = new EnrichEpisodes(
            request.TvdbId!.Value, Season: null,
            matchedIndices.Select(m => m.Candidate).ToArray(), config);

        var response = await enrichmentManager.Ask<EnrichEpisodesResponse>(enrichRequest, _enrichmentTimeout);
        if (response is not EnrichEpisodesCompleted completed)
        {
            return itemTraces;
        }

        var enrichedLookup = completed.Episodes.ToDictionary(e => e.Index);
        var result = new ItemTrace[itemTraces.Length];
        Array.Copy(itemTraces, result, itemTraces.Length);

        foreach (var (traceIndex, candidate) in matchedIndices)
        {
            var trace = result[traceIndex];
            if (enrichedLookup.TryGetValue(candidate.Index, out var enriched))
            {
                result[traceIndex] = trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        enriched.Method, enriched.Confidence, true,
                        enriched.Season, enriched.Episode, enriched.EpisodeName)
                };
            }
            else
            {
                result[traceIndex] = trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        MatchMethod.TitleMatch, 0f, false,
                        Detail: "no matching episode found")
                };
            }
        }

        return result;
    }

    private static async Task<ItemTrace[]> EnrichMovies(
        ItemTrace[] itemTraces, ApiModels.TestScoreRequest request,
        EnrichmentConfig config, IActorRef enrichmentManager)
    {
        var matchedIndices = new List<(int TraceIndex, MovieCandidate Candidate)>();

        for (var i = 0; i < itemTraces.Length; i++)
        {
            var trace = itemTraces[i];
            if (!trace.Matched)
            {
                continue;
            }

            var airedAt = trace.CandidateTimestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(trace.CandidateTimestamp)
                : (DateTimeOffset?)null;

            matchedIndices.Add((i, new MovieCandidate(
                matchedIndices.Count,
                trace.CandidateTitle,
                airedAt,
                trace.CandidateDuration)));
        }

        if (matchedIndices.Count == 0)
        {
            return itemTraces;
        }

        var enrichRequest = new EnrichMovies(
            request.ImdbId, request.TmdbId,
            matchedIndices.Select(m => m.Candidate).ToArray(), config);

        var response = await enrichmentManager.Ask<EnrichMoviesResponse>(enrichRequest, _enrichmentTimeout);
        if (response is not EnrichMoviesCompleted completed)
        {
            return itemTraces;
        }

        var enrichedLookup = completed.Movies.ToDictionary(m => m.Index);
        var result = new ItemTrace[itemTraces.Length];
        Array.Copy(itemTraces, result, itemTraces.Length);

        foreach (var (traceIndex, candidate) in matchedIndices)
        {
            var trace = result[traceIndex];
            if (enrichedLookup.TryGetValue(candidate.Index, out var enriched))
            {
                result[traceIndex] = trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        enriched.Method, enriched.Confidence, true,
                        ResolvedTitle: enriched.Title, ResolvedYear: enriched.Year)
                };
            }
            else
            {
                result[traceIndex] = trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        MatchMethod.TitleMatch, 0f, false,
                        Detail: "no matching movie found")
                };
            }
        }

        return result;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex RuleSetIdRegex();
}
