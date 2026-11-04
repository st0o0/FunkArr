using FunkArr.Messages.RuleSet;

namespace FunkArr.RuleSet.Manager;

public sealed class RuleSetManagerState
{
    public Dictionary<string, RuleSetPaths> KnownRuleSets { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> PendingIds { get; } = new(StringComparer.Ordinal);
    public bool FullRescanRequested { get; set; }
    internal Dictionary<string, WorkerSummary> Summaries { get; } = new(StringComparer.Ordinal);

    public void Apply(WorkerRemoved msg)
    {
        KnownRuleSets.Remove(msg.RuleSetId);
        Summaries.Remove(msg.RuleSetId);
    }

    public void AddPendingId(string ruleSetId) => PendingIds.Add(ruleSetId);

    public void ClearPending()
    {
        PendingIds.Clear();
        FullRescanRequested = false;
    }
}

public sealed record RuleSetPaths(
    string? CommunityPath,
    string? LocalPath,
    DateTime? CommunityModified,
    DateTime? LocalModified);
