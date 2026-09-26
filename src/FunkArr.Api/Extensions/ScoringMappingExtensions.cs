using FunkArr.Messages.Scoring.History;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api.Extensions;

internal static class ScoringMappingExtensions
{
    internal static ApiModels.ItemTrace ToApi(this ItemTrace msg) =>
        new(msg.CandidateTitle, msg.CandidateTopic, msg.CandidateChannel,
            msg.CandidateDuration, msg.CandidateQuality, msg.CandidateDescription,
            msg.CandidateTimestamp, msg.Matched, msg.Score, msg.MatchedRuleId,
            msg.Identification is not null
                ? new ApiModels.TracedIdentification(msg.Identification.Season, msg.Identification.Episode, msg.Identification.Title)
                : null,
            [.. msg.RuleTraces.Select(rt => rt.ToApi())],
            msg.EnrichmentTrace is not null
                ? new ApiModels.EnrichmentTraceOutput(
                    (ApiModels.MatchMethod)(int)msg.EnrichmentTrace.Method, msg.EnrichmentTrace.Confidence, msg.EnrichmentTrace.Enriched,
                    msg.EnrichmentTrace.ResolvedSeason, msg.EnrichmentTrace.ResolvedEpisode,
                    msg.EnrichmentTrace.ResolvedTitle, msg.EnrichmentTrace.ResolvedYear,
                    msg.EnrichmentTrace.DaysDiff, msg.EnrichmentTrace.Detail)
                : null);

    internal static ApiModels.RuleTrace ToApi(this RuleTrace msg) =>
        new(msg.RuleId, msg.Priority, msg.Outcome.ToApi(),
            msg.FilterTrace is not null ? msg.FilterTrace.ToApi() : null,
            msg.IdentificationTrace is not null
                ? new ApiModels.IdentificationTrace(msg.IdentificationTrace.Strategy?.ToString(), msg.IdentificationTrace.Attempted, msg.IdentificationTrace.Detail?.ToString())
                : null);

    internal static ApiModels.FilterGroupTrace ToApi(this FilterGroupTrace msg) =>
        new(msg.Operator.ToString(), msg.Passed,
        [
            .. msg.Nodes.Select(n => new ApiModels.FilterNodeTrace(
                n.Field, n.Op, n.ExpectedValue, n.ActualValue, n.Passed, n.Skipped,
                n.Group is not null ? n.Group.ToApi() : null))
        ]);

    internal static ApiModels.RuleOutcome ToApi(this RuleOutcome msg) => msg switch
    {
        RuleOutcome.Matched => ApiModels.RuleOutcome.Matched,
        RuleOutcome.FilterFailed => ApiModels.RuleOutcome.FilterFailed,
        RuleOutcome.IdentificationFailed => ApiModels.RuleOutcome.IdentificationFailed,
        _ => ApiModels.RuleOutcome.FilterFailed,
    };
}
