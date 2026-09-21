using System.Collections.Immutable;

namespace FunkArr.Messages.History;

public sealed record StatsUpdated(string RuleSetId, ScoringStatsResult Stats);

public sealed record QueryAllStats;

public sealed record AllStatsSnapshot(ImmutableDictionary<string, ScoringStatsResult> Entries);

public sealed record RemoveStats(string RuleSetId);
