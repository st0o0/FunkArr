using System.Collections.Immutable;

namespace FunkArr.Messages.History;

public sealed record UpdateStats(string RuleSetId, ScoringStatsResult Stats);

public sealed record QueryAllStats;

public sealed record AllStatsResult(ImmutableDictionary<string, ScoringStatsResult> Entries);

public sealed record RemoveStats(string RuleSetId);
