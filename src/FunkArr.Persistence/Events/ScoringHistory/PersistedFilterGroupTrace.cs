namespace FunkArr.Persistence.Events.ScoringHistory;

public sealed record PersistedFilterGroupTrace(
    PersistedFilterGroupOp Operator,
    bool Passed,
    PersistedFilterNodeTrace[] Nodes);
