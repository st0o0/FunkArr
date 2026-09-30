## REMOVED Requirements

### Requirement: RecentMatchActor registration
**Reason**: Absorbed into `MatchStatsActor` which provides the same query capabilities plus additional analytics projections.
**Migration**: Replace all `RecentMatchActor` references with `MatchStatsActor`. PersistenceId changes from `"recent-match-actor"` to `"match-stats"`.

### Requirement: Record match results
**Reason**: Replaced by `MatchStatsActor.RecordMatchRun`.
**Migration**: Callers now Tell `MatchStatsActor` instead of `RecentMatchActor`.

### Requirement: Recent records query
**Reason**: `GetRecent` query absorbed into `MatchStatsActor`.
**Migration**: Same query interface on `MatchStatsActor`.

### Requirement: Unmatched items aggregation
**Reason**: Unmatched tracking absorbed into `MatchStatsActor`.
**Migration**: Same query interface on `MatchStatsActor`.

### Requirement: Snapshot support
**Reason**: `MatchStatsActor` uses its own snapshot strategy.
**Migration**: New snapshot format in `MatchStatsActor`.

### Requirement: Direct fetch by evaluation ID
**Reason**: `GetById` query absorbed into `MatchStatsActor`.
**Migration**: Same query interface on `MatchStatsActor`.
