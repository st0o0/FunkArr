namespace FunkArr.Messages.Scoring.History;

public sealed record RuleTrace(
    string RuleId,
    int Priority,
    RuleOutcome Outcome,
    FilterGroupTrace? FilterTrace,
    IdentificationTrace? IdentificationTrace);
