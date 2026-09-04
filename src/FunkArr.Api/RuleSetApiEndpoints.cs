using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class RuleSetApiEndpoints
{
    private static readonly TimeSpan _queryTimeout = TimeSpan.FromSeconds(10);

    public static WebApplication MapRuleSetApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/rulesets");

        group.MapGet("/", async (IActorRegistry registry) =>
        {
            var resolver = await registry.GetAsync<IRuleSetResolver>();
            try
            {
                var result = await resolver.Ask<RegisteredRuleSetsResult>(
                    new QueryRegisteredRuleSets(), _queryTimeout);
                return Results.Ok(result.Entries.Select(e => new ApiModels.RuleSetListEntry(
                    e.RuleSetId, e.Topic, e.Aliases, e.TvdbId, e.ImdbId, e.TmdbId)).ToArray());
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        })
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
        .Produces<ApiModels.ScoringDetail>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        return app;
    }

    private static IResult GatewayTimeout() =>
        Results.Problem(statusCode: 504, title: "Gateway Timeout");

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
}
