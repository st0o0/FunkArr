using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Api.Validation;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class RuleSetApiEndpoints
{
    private static readonly TimeSpan _queryTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _statsTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(15);

    public static WebApplication MapRuleSetApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/rulesets")
            .WithTags("Rulesets")
            .AddEndpointFilter<ValidationEndpointFilter>()
            .AddEndpointFilter<EndpointExceptionFilter>();
        group.WithRequestTimeout(TimeSpan.FromSeconds(15));

        group.MapGet("/", async (IActorRegistry registry, IDataFiles dataFiles, DataPaths dataPaths, CancellationToken ct) =>
        {
            var resolver = await registry.GetAsync<IRuleSetResolver>(ct);
            var manager = await registry.GetAsync<IRuleSetManager>(ct);
            var statsCollector = await registry.GetAsync<IStatsCollector>(ct);

            var resolverTask = resolver.Ask<RegisteredRuleSetsResult>(
                new QueryRegisteredRuleSets(), _queryTimeout, ct);
            var summaryTask = manager.Ask<RuleSetSummaryResult>(
                new QueryRuleSetSummaries(), _queryTimeout, ct);
            var statsTask = statsCollector.Ask<AllStatsResult>(
                new QueryAllStats(), _statsTimeout, ct);

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
                    e.RuleSetId, e.Topic, e.Aliases,
                    e.Ids.TvdbId, e.Ids.ImdbId, e.Ids.TmdbId,
                    e.MediaName, e.MediaType is not null ? (ApiModels.MediaType)(int)e.MediaType : null,
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

        group.MapGet("/{id}", async (string id, IActorRegistry registry, CancellationToken ct) =>
        {
            var region = await registry.GetAsync<IRuleSetRegion>(ct);
            var result = await region.Ask<RuleSetDetailResponse>(
                new QueryRuleSetDetail(id), _queryTimeout, ct);
            return result switch
            {
                RuleSetDetailResult detail => Results.Ok(detail.ToApi()),
                RuleSetDetailFailed => Results.NotFound(),
                _ => ApiResults.GatewayTimeout()
            };
        })
        .WithSummary("Get ruleset details")
        .WithDescription("Returns full ruleset configuration including identity, source info, and all matching rules.")
        .Produces<ApiModels.RuleSetDetail>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapGet("/{id}/history", async (string id, int? offset, int? limit, IActorRegistry registry, CancellationToken ct) =>
        {
            var historyRegion = await registry.GetAsync<IHistoryRegion>(ct);
            var result = await historyRegion.Ask<ScoringHistoryResult>(
                new QueryScoringHistory(id, offset ?? 0, limit ?? 20), _queryTimeout, ct);
            return Results.Ok(result.ToApi());
        })
        .WithSummary("Get scoring history")
        .WithDescription("Returns paginated scoring history for a ruleset.")
        .Produces<ApiModels.ScoringHistory>()
        .ProducesProblem(504);

        group.MapGet("/{id}/history/{requestId:guid}", async (string id, Guid requestId, IActorRegistry registry, CancellationToken ct) =>
        {
            var historyRegion = await registry.GetAsync<IHistoryRegion>(ct);
            var result = await historyRegion.Ask<ScoringDetailResponse>(
                new QueryScoringDetail(id, requestId), _queryTimeout, ct);
            return result switch
            {
                ScoringDetailResult detail => Results.Ok(detail.ToApi()),
                ScoringDetailFailed => Results.NotFound(),
                _ => ApiResults.GatewayTimeout()
            };
        })
        .WithSummary("Get scoring detail")
        .WithDescription("Returns detailed scoring trace for a specific scoring request, including per-item and per-rule traces.")
        .Produces<ApiModels.ScoringDetail>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapPost("/", HandleCreate)
            .WithSummary("Create local ruleset")
            .WithDescription("Creates a new local ruleset from the provided configuration. Returns 409 if a ruleset with the same ID already exists.")
            .Produces<ApiModels.CreatedRuleSetResponse>(201)
            .ProducesProblem(400)
            .ProducesProblem(409)
            .Produces<ApiModels.ValidationErrorResponse>(422);
        group.MapPut("/{id}", HandleUpdate)
            .WithSummary("Update ruleset")
            .WithDescription("Replaces the configuration of an existing local ruleset.")
            .Produces(200)
            .ProducesProblem(404)
            .Produces<ApiModels.ValidationErrorResponse>(422);
        group.MapDelete("/{id}", HandleDelete)
            .WithSummary("Delete local ruleset")
            .WithDescription("Permanently removes a local ruleset. Community rulesets cannot be deleted.")
            .Produces(200)
            .ProducesProblem(404);
        group.MapGet("/{id}/raw", HandleGetRaw)
            .WithSummary("Get raw ruleset JSON")
            .WithDescription("Returns the raw JSON definition of a ruleset as stored on disk.")
            .Produces(200, contentType: "application/json")
            .ProducesProblem(404);

        group.MapGet("/{id}/export", HandleExport)
            .WithSummary("Export ruleset for community contribution")
            .WithDescription("Downloads the ruleset as a JSON file suitable for contributing to the community repository.")
            .Produces(200, contentType: "application/json")
            .ProducesProblem(404)
            .Produces<ApiModels.ValidationErrorResponse>(422);

        group.MapPost("/test", async (ApiModels.TestScoreRequest request, IActorRegistry registry, CancellationToken ct) =>
        {
            var (config, candidates) = request.ToMessage();

            var manager = await registry.GetAsync<IScoringManager>(ct);
            var result = await manager.Ask<TestScoreItemsResponse>(
                new TestScoreItems(Guid.NewGuid(), config, candidates), _testTimeout, ct);

            if (result is not TestScoreCompleted completed)
            {
                return ApiResults.GatewayTimeout();
            }

            var itemTraces = completed.ItemTraces;
            if (request.Enrichment is not null && request.Enrichment.Enabled != false)
            {
                itemTraces = await RunTestEnrichment(itemTraces, request, registry, ct);
            }

            return Results.Ok(new ApiModels.TestScoreResponse(
                [.. itemTraces.Select(t => t.ToApi())]));
        })
        .WithSummary("Test ruleset scoring")
        .WithDescription("Runs scoring against provided candidates using an ad-hoc ruleset configuration with optional enrichment. Returns per-item traces.")
        .Produces<ApiModels.TestScoreResponse>()
        .ProducesProblem(400)
        .ProducesProblem(504);

        return app;
    }

    private static async Task EvictRuleSetCache(IOutputCacheStore cache, CancellationToken ct) => await cache.EvictByTagAsync("rulesets", ct);

    private static async Task<IResult> HandleCreate(ApiModels.CreateRuleSetRequest request, IActorRegistry registry, IOutputCacheStore cache, CancellationToken ct)
    {
        var region = await registry.GetAsync<IRuleSetRegion>(ct);
        var result = await region.Ask<CreateLocalRuleSetResponse>(request.ToCommand(), _queryTimeout, ct);

        await EvictRuleSetCache(cache, ct);
        return result switch
        {
            CreateLocalRuleSetCompleted completed => Results.Created($"/api/rulesets/{completed.RuleSetId}", new ApiModels.CreatedRuleSetResponse(completed.RuleSetId)),
            CreateLocalRuleSetFailed { Reason: CreateLocalRuleSetFailureReason.AlreadyExists } => Results.Conflict(new ApiModels.ErrorResponse($"Local ruleset '{request.RuleSetId}' already exists")),
            CreateLocalRuleSetValidationFailed failed => Results.UnprocessableEntity(new ApiModels.ValidationErrorResponse(failed.Errors.ToApiErrors())),
            _ => ApiResults.GatewayTimeout()
        };
    }

    private static async Task<IResult> HandleUpdate(string id, ApiModels.UpdateRuleSetRequest request, IActorRegistry registry, IOutputCacheStore cache, CancellationToken ct)
    {
        var region = await registry.GetAsync<IRuleSetRegion>(ct);
        var result = await region.Ask<UpdateLocalRuleSetResponse>(request.ToCommand(id), _queryTimeout, ct);

        await EvictRuleSetCache(cache, ct);
        return result switch
        {
            UpdateLocalRuleSetCompleted => Results.Ok(),
            UpdateLocalRuleSetFailed { Reason: UpdateLocalRuleSetFailureReason.NotFound } => Results.NotFound(),
            UpdateLocalRuleSetValidationFailed failed => Results.UnprocessableEntity(new ApiModels.ValidationErrorResponse(failed.Errors.ToApiErrors())),
            _ => ApiResults.GatewayTimeout()
        };
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

    private static async Task<IResult> HandleDelete(string id, IActorRegistry registry, IOutputCacheStore cache, CancellationToken ct)
    {
        var region = await registry.GetAsync<IRuleSetRegion>(ct);
        var result = await region.Ask<DeleteLocalRuleSetResponse>(new DeleteLocalRuleSet(id), _queryTimeout, ct);

        await EvictRuleSetCache(cache, ct);
        return result switch
        {
            DeleteLocalRuleSetCompleted => Results.Ok(),
            DeleteLocalRuleSetFailed => Results.NotFound(),
            _ => ApiResults.GatewayTimeout()
        };
    }

    private static async Task<IResult> HandleExport(string id, IActorRegistry registry, HttpContext httpContext, CancellationToken ct)
    {
        var region = await registry.GetAsync<IRuleSetRegion>(ct);
        var result = await region.Ask<ExportRuleSetResponse>(new ExportRuleSet(id), _queryTimeout, ct);

        return result switch
        {
            ExportRuleSetCompleted completed => ExportResult(id, completed.Json, httpContext),
            ExportRuleSetValidationFailed failed => Results.UnprocessableEntity(new ApiModels.ValidationErrorResponse(failed.Errors.ToApiErrors())),
            ExportRuleSetFailed { Reason: ExportRuleSetFailureReason.NoLocalOverlay } =>
                Results.Problem(statusCode: 400, title: "No local overlay",
                    detail: "Only locally modified rulesets can be exported. Edit this ruleset first to create a local overlay."),
            ExportRuleSetFailed => Results.NotFound(),
            _ => ApiResults.GatewayTimeout()
        };
    }

    private static IResult ExportResult(string id, string json, HttpContext httpContext)
    {
        httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{id}.json\"";
        return Results.Content(json, "application/json");
    }

    private static readonly TimeSpan _enrichmentTimeout = TimeSpan.FromSeconds(10);

    private static async Task<ItemTrace[]> RunTestEnrichment(
        ItemTrace[] itemTraces, ApiModels.TestScoreRequest request, IActorRegistry registry, CancellationToken ct)
    {
        var enrichmentConfig = request.Enrichment!.ToMessage();
        var isShow = string.Equals(request.MediaType, "show", StringComparison.OrdinalIgnoreCase);
        var isMovie = string.Equals(request.MediaType, "movie", StringComparison.OrdinalIgnoreCase);

        try
        {
            var enrichmentManager = await registry.GetAsync<IEnrichmentManager>(ct);

            if (isShow && request.TvdbId is not null)
            {
                return await EnrichEpisodes(itemTraces, request, enrichmentConfig, enrichmentManager, ct);
            }

            if (isMovie && (request.TmdbId is not null || request.ImdbId is not null))
            {
                return await EnrichMovies(itemTraces, request, enrichmentConfig, enrichmentManager, ct);
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
        EnrichmentConfig config, IActorRef enrichmentManager, CancellationToken ct)
    {
        var matchedIndices = new List<(int TraceIndex, EpisodeCandidate Candidate)>();

        for (var i = 0; i < itemTraces.Length; i++)
        {
            var trace = itemTraces[i];
            if (!trace.Matched)
            {
                continue;
            }

            var airedAt = trace.Candidate.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(trace.Candidate.Timestamp)
                : (DateTimeOffset?)null;

            matchedIndices.Add((i, new EpisodeCandidate(
                matchedIndices.Count,
                trace.Candidate.Title,
                trace.Identification?.Title,
                airedAt,
                trace.Candidate.Duration,
                trace.Identification?.Season,
                trace.Identification?.Episode)));
        }

        if (matchedIndices.Count == 0)
        {
            return itemTraces;
        }

        var enrichRequest = new EnrichEpisodes(
            request.TvdbId!.Value, Season: null,
            [.. matchedIndices.Select(m => m.Candidate)], config);

        var response = await enrichmentManager.Ask<EnrichEpisodesResponse>(enrichRequest, _enrichmentTimeout, ct);
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
        EnrichmentConfig config, IActorRef enrichmentManager, CancellationToken ct)
    {
        var matchedIndices = new List<(int TraceIndex, MovieCandidate Candidate)>();

        for (var i = 0; i < itemTraces.Length; i++)
        {
            var trace = itemTraces[i];
            if (!trace.Matched)
            {
                continue;
            }

            var airedAt = trace.Candidate.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(trace.Candidate.Timestamp)
                : (DateTimeOffset?)null;

            matchedIndices.Add((i, new MovieCandidate(
                matchedIndices.Count,
                trace.Candidate.Title,
                airedAt,
                trace.Candidate.Duration)));
        }

        if (matchedIndices.Count == 0)
        {
            return itemTraces;
        }

        var enrichRequest = new EnrichMovies(
            request.ImdbId, request.TmdbId,
            [.. matchedIndices.Select(m => m.Candidate)], config);

        var response = await enrichmentManager.Ask<EnrichMoviesResponse>(enrichRequest, _enrichmentTimeout, ct);
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
}
