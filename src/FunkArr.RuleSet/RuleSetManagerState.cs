using System.Collections.Immutable;

namespace FunkArr.RuleSet;

public sealed record RuleSetManagerState(
    ImmutableDictionary<string, RuleSetPaths> KnownRuleSets,
    ImmutableHashSet<string> PendingIds,
    bool FullRescanRequested)
{
    public static readonly RuleSetManagerState Empty = new(
        ImmutableDictionary<string, RuleSetPaths>.Empty.WithComparers(StringComparer.Ordinal),
        ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
        false);
}

public sealed record RuleSetPaths(
    string? CommunityPath,
    string? LocalPath,
    DateTime? CommunityModified,
    DateTime? LocalModified);
