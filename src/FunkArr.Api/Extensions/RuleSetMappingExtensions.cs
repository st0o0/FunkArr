using FunkArr.Api.Models.RuleSets;
using FunkArr.Api.Models.Scoring;
using FunkArr.Api.Models.Shared;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;
using EnrichmentMethod = FunkArr.Api.Models.RuleSets.EnrichmentMethod;
using MsgScoring = FunkArr.Messages.Scoring;
using RuleSetDetailRule = FunkArr.Api.Models.RuleSets.RuleSetDetailRule;
using RuntimeMode = FunkArr.Messages.Enrichment.RuntimeMode;
using ScoringSnapshotSummary = FunkArr.Api.Models.Scoring.ScoringSnapshotSummary;

namespace FunkArr.Api.Extensions;

internal static class RuleSetMappingExtensions
{
    internal static RuleSetDetail ToApi(this RuleSetDetailResult msg) =>
        new(msg.RuleSetId,
            new RuleSetDetail.RuleSetIdentity(
                msg.Identity.Topic, msg.Identity.Aliases,
                msg.Identity.Ids.TvdbId, msg.Identity.Ids.ImdbId, msg.Identity.Ids.TmdbId),
            new RuleSetDetail.RuleSetSource(
                msg.Source.CommunityPath, msg.Source.LocalPath,
                msg.Source.CommunityModified, msg.Source.LocalModified),
            msg.DefaultConfidence,
            [
                .. msg.Rules.Select(r => new RuleSetDetailRule(
                    r.Id, r.Priority, r.Confidence,
                    r.Strategy.ToApi(),
                    r.SeasonRegex, r.EpisodeRegex, r.CaptureGroup,
                    r.Filters.ToApi(), r.TitleRules?.Select(t => t.ToApi()).ToArray()))
            ],
            msg.Enrichment.ToApi());

    internal static FilterGroupOutput? ToApi(this MsgScoring.FilterGroupOutput? output) =>
        output is null
            ? null
            : new FilterGroupOutput(
                output.All?.Select(c => new FilterConditionOutput(
                    c.Field.ToApi(), c.Op.ToApi(), c.Value)).ToArray(),
                output.Any?.Select(c => new FilterConditionOutput(
                    c.Field.ToApi(), c.Op.ToApi(), c.Value)).ToArray(),
                output.Not?.Select(c => new FilterConditionOutput(
                    c.Field.ToApi(), c.Op.ToApi(), c.Value)).ToArray());

    internal static TitleRuleOutput ToApi(this MsgScoring.TitleRuleOutput output) =>
        new(output.Type.ToApi(),
            output.Field?.ToApi(),
            output.Pattern, output.CaptureGroup, output.Value);

    internal static EnrichmentConfigOutput ToApi(this EnrichmentConfig config) =>
        new(config.Enabled,
            [.. config.Methods.Select(m => m.ToApi())],
            new TitleMatchConfigOutput(config.Title.Threshold),
            new AirdateMatchConfigOutput(config.Airdate.Tolerance, config.Airdate.MinTitleAffinity),
            new RuntimeMatchConfigOutput(config.Runtime.Tolerance, config.Runtime.Mode.ToApi()),
            new YearMatchConfigOutput(config.Year.Tolerance));

    internal static CreateLocalRuleSet ToCommand(this CreateRuleSetRequest request) =>
        new(request.RuleSetId, ToBody(request, request.Enrichment));

    internal static UpdateLocalRuleSet ToCommand(this UpdateRuleSetRequest request, string id) =>
        new(id, ToBody(request, request.Enrichment));

    private static RuleSetBody ToBody(IRuleSetBody request, EnrichmentConfigInput? enrichment) =>
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

    private static RuleSetFilterGroupInput MapFilters(FilterGroupInput input) =>
        new(input.All?.Select(MapCondition).ToArray(),
            input.Any?.Select(MapCondition).ToArray(),
            input.Not?.Select(MapCondition).ToArray());

    private static RuleSetFilterConditionInput MapCondition(FilterNodeInput c) =>
        new((c.Field ?? FilterField.Title).ToMessage(),
            (c.Op ?? FilterOp.Eq).ToMessage(),
            c.Value ?? "");

    internal static EnrichmentConfig ToMessage(this EnrichmentConfigInput input) =>
        new(input.Enabled ?? true,
            [
                .. (input.Methods ?? [EnrichmentMethod.Title, EnrichmentMethod.Airdate])
                .Select(m => m.ToMessage())
            ],
            new TitleMatchConfig(input.Title?.Threshold ?? 0.7f),
            new AirdateMatchConfig(input.Airdate?.Tolerance ?? 7, input.Airdate?.MinTitleAffinity ?? 0.3f),
            new RuntimeMatchConfig(
                input.Runtime?.Tolerance ?? 0.35f,
                input.Runtime?.Mode?.ToMessage() ?? RuntimeMode.Tiebreaker),
            new YearMatchConfig(input.Year?.Tolerance ?? 1));

    internal static ScoringHistory ToApi(this ScoringHistoryResult msg) =>
        new(msg.RuleSetId, msg.TotalCount,
        [
            .. msg.Snapshots.Select(s => new ScoringSnapshotSummary(
                s.RequestId, s.Source.ToApi(), s.Query, s.Timestamp,
                s.CandidateCount, s.MatchedCount))
        ]);

    internal static ScoringDetail ToApi(this ScoringDetailResult msg) =>
        new(msg.RequestId, msg.Source.ToApi(), msg.Query, msg.Timestamp,
            [.. msg.ItemTraces.Select(t => t.ToApi())]);

    internal static SourceType ToApi(this string? sourceType) => sourceType switch
    {
        "community" => SourceType.Community,
        "local" => SourceType.Local,
        "merged" => SourceType.Merged,
        _ => SourceType.Unknown
    };

    internal static IReadOnlyList<ValidationErrorEntry> ToApiErrors(
        this IReadOnlyList<RuleSetValidationError> errors) =>
        [.. errors.Select(e => new ValidationErrorEntry(e.Field, e.Message))];
}
