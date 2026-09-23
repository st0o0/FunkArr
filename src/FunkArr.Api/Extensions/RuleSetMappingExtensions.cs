using FunkArr.Messages.Enrichment;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using ApiModels = FunkArr.Api.Models;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Extensions;

internal static class RuleSetMappingExtensions
{
    internal static ApiModels.RuleSetDetail ToApi(this RuleSetDetailResult msg) =>
        new(msg.RuleSetId,
            new ApiModels.RuleSetDetail.RuleSetIdentity(
                msg.Identity.Topic, msg.Identity.Aliases,
                msg.Identity.TvdbId, msg.Identity.ImdbId, msg.Identity.TmdbId),
            new ApiModels.RuleSetDetail.RuleSetSource(
                msg.Source.CommunityPath, msg.Source.LocalPath,
                msg.Source.CommunityModified, msg.Source.LocalModified),
            msg.DefaultConfidence,
            msg.Rules.Select(r => new ApiModels.RuleSetDetailRule(
                r.Id, r.Priority, r.Confidence,
                (ApiModels.IdentificationStrategy)(int)r.Strategy,
                r.SeasonRegex, r.EpisodeRegex, r.CaptureGroup,
                r.Filters.ToApi(), r.TitleRules?.Select(t => t.ToApi()).ToArray())).ToArray(),
            msg.Enrichment.ToApi());

    internal static ApiModels.FilterGroupOutput? ToApi(this MsgScoring.FilterGroupOutput? output) =>
        output is null
            ? null
            : new ApiModels.FilterGroupOutput(
                output.All?.Select(c => new ApiModels.FilterConditionOutput(
                    (ApiModels.FilterField)(int)c.Field, (ApiModels.FilterOp)(int)c.Op, c.Value)).ToArray(),
                output.Any?.Select(c => new ApiModels.FilterConditionOutput(
                    (ApiModels.FilterField)(int)c.Field, (ApiModels.FilterOp)(int)c.Op, c.Value)).ToArray(),
                output.Not?.Select(c => new ApiModels.FilterConditionOutput(
                    (ApiModels.FilterField)(int)c.Field, (ApiModels.FilterOp)(int)c.Op, c.Value)).ToArray());

    internal static ApiModels.TitleRuleOutput ToApi(this MsgScoring.TitleRuleOutput output) =>
        new((ApiModels.TitlePartType)(int)output.Type,
            output.Field is null ? null : (ApiModels.FilterField)(int)output.Field,
            output.Pattern, output.CaptureGroup, output.Value);

    internal static ApiModels.EnrichmentConfigOutput ToApi(this EnrichmentConfig config) =>
        new(config.Enabled,
            config.Methods.Select(m => (ApiModels.EnrichmentMethod)(int)m).ToArray(),
            new ApiModels.TitleMatchConfigOutput(config.Title.Threshold),
            new ApiModels.AirdateMatchConfigOutput(config.Airdate.Tolerance, config.Airdate.MinTitleAffinity),
            new ApiModels.RuntimeMatchConfigOutput(config.Runtime.Tolerance, (ApiModels.RuntimeMode)(int)config.Runtime.Mode),
            new ApiModels.YearMatchConfigOutput(config.Year.Tolerance));

    internal static EnrichmentConfig ToMessage(this ApiModels.EnrichmentConfigInput input) =>
        new(input.Enabled ?? true,
            (input.Methods ?? [ApiModels.EnrichmentMethod.Title, ApiModels.EnrichmentMethod.Airdate])
                .Select(m => (EnrichmentMethod)(int)m).ToArray(),
            new TitleMatchConfig(input.Title?.Threshold ?? 0.7f),
            new AirdateMatchConfig(input.Airdate?.Tolerance ?? 7, input.Airdate?.MinTitleAffinity ?? 0.3f),
            new RuntimeMatchConfig(
                input.Runtime?.Tolerance ?? 0.35f,
                input.Runtime?.Mode is not null ? (RuntimeMode)(int)input.Runtime.Mode : RuntimeMode.Tiebreaker),
            new YearMatchConfig(input.Year?.Tolerance ?? 1));

    internal static ApiModels.ScoringHistory ToApi(this ScoringHistoryResult msg) =>
        new(msg.RuleSetId, msg.TotalCount,
            msg.Snapshots.Select(s => new ApiModels.ScoringSnapshotSummary(
                s.RequestId, (ApiModels.SearchSource)(int)s.Source, s.Query, s.Timestamp,
                s.CandidateCount, s.MatchedCount)).ToArray());

    internal static ApiModels.ScoringDetail ToApi(this ScoringDetailResult msg) =>
        new(msg.RequestId, (ApiModels.SearchSource)(int)msg.Source, msg.Query, msg.Timestamp,
            msg.ItemTraces.Select(t => t.ToApi()).ToArray());

    internal static ApiModels.SourceType ToApi(this string? sourceType) => sourceType switch
    {
        "community" => ApiModels.SourceType.Community,
        "local" => ApiModels.SourceType.Local,
        "merged" => ApiModels.SourceType.Merged,
        _ => ApiModels.SourceType.Unknown,
    };
}
