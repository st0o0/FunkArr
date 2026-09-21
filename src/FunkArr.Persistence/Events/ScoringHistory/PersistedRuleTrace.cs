namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedRuleTrace(
    string RuleId,
    int Priority,
    PersistedRuleOutcome Outcome,
    PersistedFilterGroupTrace? FilterTrace,
    PersistedIdentificationTrace? IdentificationTrace);
