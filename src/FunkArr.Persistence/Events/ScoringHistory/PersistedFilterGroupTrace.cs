namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedFilterGroupTrace(
    string Operator,
    bool Passed,
    PersistedFilterNodeTrace[] Nodes);
