using FunkArr.Messages.Enrichment;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring.History;
using ApiModels = FunkArr.Api.Models;

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
                r.Id, r.Priority, r.Confidence, r.Strategy,
                r.SeasonRegex, r.EpisodeRegex, r.CaptureGroup,
                r.Filters, r.TitleRules)).ToArray(),
            msg.Enrichment.ToApi());

    internal static ApiModels.EnrichmentConfigOutput ToApi(this EnrichmentConfig config) =>
        new(config.Enabled, config.Methods,
            new ApiModels.TitleMatchConfigOutput(config.Title.Threshold),
            new ApiModels.AirdateMatchConfigOutput(config.Airdate.Tolerance),
            new ApiModels.RuntimeMatchConfigOutput(config.Runtime.Tolerance, config.Runtime.Mode),
            new ApiModels.YearMatchConfigOutput(config.Year.Tolerance));

    internal static EnrichmentConfig ToMessage(this ApiModels.EnrichmentConfigInput input) =>
        new(input.Enabled ?? true,
            input.Methods ?? [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
            new TitleMatchConfig(input.Title?.Threshold ?? 0.7f),
            new AirdateMatchConfig(input.Airdate?.Tolerance ?? 7),
            new RuntimeMatchConfig(input.Runtime?.Tolerance ?? 0.35f, input.Runtime?.Mode ?? RuntimeMode.Tiebreaker),
            new YearMatchConfig(input.Year?.Tolerance ?? 1));

    internal static ApiModels.ScoringHistory ToApi(this ScoringHistoryResult msg) =>
        new(msg.RuleSetId, msg.TotalCount,
            msg.Snapshots.Select(s => new ApiModels.ScoringSnapshotSummary(
                s.RequestId, s.Source, s.Query, s.Timestamp,
                s.CandidateCount, s.MatchedCount)).ToArray());

    internal static ApiModels.ScoringDetail ToApi(this ScoringDetailResult msg) =>
        new(msg.RequestId, msg.Source, msg.Query, msg.Timestamp,
            msg.ItemTraces.Select(t => t.ToApi()).ToArray());

    internal static ApiModels.SourceType ToApi(this string? sourceType) => sourceType switch
    {
        "community" => ApiModels.SourceType.Community,
        "local" => ApiModels.SourceType.Local,
        "merged" => ApiModels.SourceType.Merged,
        _ => ApiModels.SourceType.Unknown,
    };

}
