using FunkArr.Messages.Enrichment;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;
using ApiModels = FunkArr.Api.Models;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Extensions;

internal static class RuleSetMappingExtensions
{
    internal static ApiModels.RuleSetDetail ToApi(this RuleSetDetailResult msg) =>
        new(msg.RuleSetId,
            new ApiModels.RuleSetDetail.RuleSetIdentity(
                msg.Identity.Topic, msg.Identity.Aliases,
                msg.Identity.Ids.TvdbId, msg.Identity.Ids.ImdbId, msg.Identity.Ids.TmdbId),
            new ApiModels.RuleSetDetail.RuleSetSource(
                msg.Source.CommunityPath, msg.Source.LocalPath,
                msg.Source.CommunityModified, msg.Source.LocalModified),
            msg.DefaultConfidence,
            [
                .. msg.Rules.Select(r => new ApiModels.RuleSetDetailRule(
                    r.Id, r.Priority, r.Confidence,
                    r.Strategy.ToApi(),
                    r.SeasonRegex, r.EpisodeRegex, r.CaptureGroup,
                    r.Filters.ToApi(), r.TitleRules?.Select(t => t.ToApi()).ToArray()))
            ],
            msg.Enrichment.ToApi());

    internal static ApiModels.FilterGroupOutput? ToApi(this MsgScoring.FilterGroupOutput? output) =>
        output is null
            ? null
            : new ApiModels.FilterGroupOutput(
                output.All?.Select(c => new ApiModels.FilterConditionOutput(
                    c.Field.ToApi(), c.Op.ToApi(), c.Value)).ToArray(),
                output.Any?.Select(c => new ApiModels.FilterConditionOutput(
                    c.Field.ToApi(), c.Op.ToApi(), c.Value)).ToArray(),
                output.Not?.Select(c => new ApiModels.FilterConditionOutput(
                    c.Field.ToApi(), c.Op.ToApi(), c.Value)).ToArray());

    internal static ApiModels.TitleRuleOutput ToApi(this MsgScoring.TitleRuleOutput output) =>
        new(output.Type.ToApi(),
            output.Field?.ToApi(),
            output.Pattern, output.CaptureGroup, output.Value);

    internal static ApiModels.EnrichmentConfigOutput ToApi(this EnrichmentConfig config) =>
        new(config.Enabled,
            [.. config.Methods.Select(m => m.ToApi())],
            new ApiModels.TitleMatchConfigOutput(config.Title.Threshold),
            new ApiModels.AirdateMatchConfigOutput(config.Airdate.Tolerance, config.Airdate.MinTitleAffinity),
            new ApiModels.RuntimeMatchConfigOutput(config.Runtime.Tolerance, config.Runtime.Mode.ToApi()),
            new ApiModels.YearMatchConfigOutput(config.Year.Tolerance));

    internal static CreateLocalRuleSet ToCommand(this ApiModels.CreateRuleSetRequest request) =>
        new(request.RuleSetId, ToBody(request, request.Enrichment));

    internal static UpdateLocalRuleSet ToCommand(this ApiModels.UpdateRuleSetRequest request, string id) =>
        new(id, ToBody(request, request.Enrichment));

    private static RuleSetBody ToBody(ApiModels.IRuleSetBody request, ApiModels.EnrichmentConfigInput? enrichment) =>
        new(request.Topic,
            request.Aliases,
            new RuleSetMediaInput(
                request.Media.Name,
                request.Media.Type.ToMessage(),
                request.Media.TvdbId is not null || request.Media.ImdbId is not null || request.Media.TmdbId is not null
                    ? new ExternalIds(request.Media.TvdbId, request.Media.ImdbId, request.Media.TmdbId)
                    : null),
            request.Confidence,
            [
                .. request.Rules.Select(r => new RuleSetRuleInput(
                    r.Id, r.Priority, r.Confidence,
                    r.Strategy?.ToMessage(),
                    r.SeasonRegex, r.EpisodeRegex, r.CaptureGroup,
                    r.Filters is not null ? MapFilters(r.Filters) : null,
                    r.TitleRules?.Select(t => new RuleSetTitleRuleInput(
                        t.Type.ToMessage(),
                        t.Field?.ToMessage(),
                        t.Pattern, t.CaptureGroup, t.Value)).ToArray()))
            ],
            request.Standalone, request.Disable,
            enrichment?.ToMessage());

    private static RuleSetFilterGroupInput MapFilters(ApiModels.FilterGroupInput input) =>
        new(input.All?.Select(MapCondition).ToArray(),
            input.Any?.Select(MapCondition).ToArray(),
            input.Not?.Select(MapCondition).ToArray());

    private static RuleSetFilterConditionInput MapCondition(ApiModels.FilterNodeInput c) =>
        new((c.Field ?? ApiModels.FilterField.Title).ToMessage(),
            (c.Op ?? ApiModels.FilterOp.Eq).ToMessage(),
            c.Value ?? "");

    internal static EnrichmentConfig ToMessage(this ApiModels.EnrichmentConfigInput input) =>
        new(input.Enabled ?? true,
            [
                .. (input.Methods ?? [ApiModels.EnrichmentMethod.Title, ApiModels.EnrichmentMethod.Airdate])
                .Select(m => m.ToMessage())
            ],
            new TitleMatchConfig(input.Title?.Threshold ?? 0.7f),
            new AirdateMatchConfig(input.Airdate?.Tolerance ?? 7, input.Airdate?.MinTitleAffinity ?? 0.3f),
            new RuntimeMatchConfig(
                input.Runtime?.Tolerance ?? 0.35f,
                input.Runtime?.Mode?.ToMessage() ?? RuntimeMode.Tiebreaker),
            new YearMatchConfig(input.Year?.Tolerance ?? 1));

    internal static ApiModels.ScoringHistory ToApi(this ScoringHistoryResult msg) =>
        new(msg.RuleSetId, msg.TotalCount,
        [
            .. msg.Snapshots.Select(s => new ApiModels.ScoringSnapshotSummary(
                s.RequestId, s.Source.ToApi(), s.Query, s.Timestamp,
                s.CandidateCount, s.MatchedCount))
        ]);

    internal static ApiModels.ScoringDetail ToApi(this ScoringDetailResult msg) =>
        new(msg.RequestId, msg.Source.ToApi(), msg.Query, msg.Timestamp,
            [.. msg.ItemTraces.Select(t => t.ToApi())]);

    internal static ApiModels.SourceType ToApi(this string? sourceType) => sourceType switch
    {
        "community" => ApiModels.SourceType.Community,
        "local" => ApiModels.SourceType.Local,
        "merged" => ApiModels.SourceType.Merged,
        _ => ApiModels.SourceType.Unknown
    };

    internal static IReadOnlyList<ApiModels.ValidationErrorEntry> ToApiErrors(
        this IReadOnlyList<RuleSetValidationError> errors) =>
        [.. errors.Select(e => new ApiModels.ValidationErrorEntry(e.Field, e.Message))];
}
