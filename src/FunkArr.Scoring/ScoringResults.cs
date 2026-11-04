using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Scoring;

public sealed record ScoringResult(ScoredItem[] Scored, ItemTrace[] Traces);

internal sealed record FilterEvaluation(bool Passed, FilterGroupTrace? Trace);

internal sealed record NodeEvaluation(bool Passed, FilterNodeTrace Trace);

internal sealed record ConditionResult(bool Passed, string? ActualValue);

internal sealed record IdentificationResult(
    bool Success,
    IdentificationTrace Trace,
    TracedIdentification? Identification)
{
    public static IdentificationResult Matched(
        IdentificationStrategy strategy, TracedIdentification identification) =>
        new(true, new IdentificationTrace(strategy, true, null), identification);

    public static IdentificationResult Failed(
        IdentificationStrategy? strategy, IdentificationFailureReason reason) =>
        new(false, new IdentificationTrace(strategy, true, reason), null);
}
