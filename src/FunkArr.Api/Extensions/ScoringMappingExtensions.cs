using FunkArr.Api.Models.RuleSets;
using FunkArr.Api.Models.Scoring;
using FunkArr.Messages.Scoring;
using FilterField = FunkArr.Api.Models.RuleSets.FilterField;
using FilterGroupTrace = FunkArr.Messages.Scoring.History.FilterGroupTrace;
using FilterNodeTrace = FunkArr.Api.Models.Scoring.FilterNodeTrace;
using FilterOp = FunkArr.Api.Models.RuleSets.FilterOp;
using ItemTrace = FunkArr.Messages.Scoring.History.ItemTrace;
using RuleOutcome = FunkArr.Messages.Scoring.History.RuleOutcome;
using RuleTrace = FunkArr.Messages.Scoring.History.RuleTrace;
using ScoreCandidate = FunkArr.Api.Models.Scoring.ScoreCandidate;
using TracedIdentification = FunkArr.Api.Models.Scoring.TracedIdentification;

namespace FunkArr.Api.Extensions;

internal static class ScoringMappingExtensions
{
    internal static Models.Scoring.ItemTrace ToApi(this ItemTrace msg) =>
        new(new ScoreCandidate(
                msg.Candidate.Title, msg.Candidate.Topic, msg.Candidate.Channel,
                msg.Candidate.Duration, msg.Candidate.Quality, msg.Candidate.Description,
                msg.Candidate.Timestamp),
            msg.Matched, msg.Score, msg.MatchedRuleId,
            msg.Identification is not null
                ? new TracedIdentification(msg.Identification.Season, msg.Identification.Episode, msg.Identification.Title)
                : null,
            [.. msg.RuleTraces.Select(rt => rt.ToApi())],
            msg.EnrichmentTrace is not null
                ? new EnrichmentTraceOutput(
                    msg.EnrichmentTrace.Method.ToApi(), msg.EnrichmentTrace.Confidence, msg.EnrichmentTrace.Enriched,
                    msg.EnrichmentTrace.ResolvedSeason, msg.EnrichmentTrace.ResolvedEpisode,
                    msg.EnrichmentTrace.ResolvedTitle, msg.EnrichmentTrace.ResolvedYear,
                    msg.EnrichmentTrace.DaysDiff, msg.EnrichmentTrace.Detail)
                : null,
            msg.ExcludedVariant is { } excluded ? excluded.ToApi() : null);

    internal static Models.Scoring.RuleTrace ToApi(this RuleTrace msg) =>
        new(msg.RuleId, msg.Priority, msg.Outcome.ToApi(),
            msg.FilterTrace?.ToApi(),
            msg.IdentificationTrace is not null
                ? new Models.Scoring.IdentificationTrace(
                    msg.IdentificationTrace.Strategy?.ToApi(),
                    msg.IdentificationTrace.Attempted,
                    msg.IdentificationTrace.Detail?.ToString())
                : null);

    internal static Models.Scoring.FilterGroupTrace ToApi(this FilterGroupTrace msg) =>
        new(msg.Operator.ToApi(), msg.Passed,
        [
            .. msg.Nodes.Select(n => new FilterNodeTrace(
                ParseFilterField(n.Field), ParseFilterOp(n.Op),
                n.ExpectedValue, n.ActualValue, n.Passed, n.Skipped,
                n.Group?.ToApi()))
        ]);

    internal static Models.Scoring.RuleOutcome ToApi(this RuleOutcome msg) => msg switch
    {
        RuleOutcome.Matched => Models.Scoring.RuleOutcome.Matched,
        RuleOutcome.FilterFailed => Models.Scoring.RuleOutcome.FilterFailed,
        RuleOutcome.IdentificationFailed => Models.Scoring.RuleOutcome.IdentificationFailed,
        _ => Models.Scoring.RuleOutcome.FilterFailed
    };

    internal static FilterOperator ToApi(this FilterGroupOp value) => value switch
    {
        FilterGroupOp.All => FilterOperator.All,
        FilterGroupOp.Any => FilterOperator.Any,
        FilterGroupOp.Not => FilterOperator.Not,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static FilterField? ParseFilterField(string? value) =>
        value is not null && Enum.TryParse<FilterField>(value, true, out var parsed) ? parsed : null;

    private static FilterOp? ParseFilterOp(string? value) =>
        value is not null && Enum.TryParse<FilterOp>(value, true, out var parsed) ? parsed : null;
}
