## REMOVED Requirements

### Requirement: ShowActor sharded persistent actor
**Reason**: Replaced by `SeriesRuleSetActor` extending `MediaRuleSetActor`. See `media-ruleset-actor` spec.
**Migration**: All references to `ShowActor` SHALL be updated to `SeriesRuleSetActor`. Shard region registration updated. PersistenceId changes from `"show-{tvdbId}"` to `"ruleset-series-{tvdbId}"`.

### Requirement: Show identity resolution
**Reason**: Metadata caching removed from entity. Gateway is the single cache.
**Migration**: `ResolveMetadata` hook in `SeriesRuleSetActor` asks `TvdbGatewayActor` on each request. No transient cache in entity.

### Requirement: ResolveSearch message
**Reason**: Moved to `SeriesRuleSetActor` subclass with `BuildSearchHint` hook.
**Migration**: `ResolveSearch` handled by `SeriesRuleSetActor` via hook delegation.

### Requirement: Match message
**Reason**: Match pipeline moved to `MediaRuleSetActor` base class with `SelectStrategies` hook.
**Migration**: `SeriesRuleSetActor.SelectStrategies` dispatches to `RuleSetMatchingEngine.EvaluateRulesWithTraces`.

### Requirement: Match handler produces full traces
**Reason**: Trace emission moved to `MediaRuleSetActor` base. Stats forwarding changed from `RecentMatchActor` to `MatchStatsActor`.
**Migration**: Base class Tells `MatchStatsActor.RecordMatchRun` instead of `RecentMatchActor.RecordSearchEvaluation`.

### Requirement: Ruleset ownership
**Reason**: Three-layer management moved to `MediaRuleSetActor` base class.
**Migration**: Same behavior, now in base class.

### Requirement: Match quality tracking
**Reason**: Analytics moved to `MatchStatsActor`. Entity no longer tracks per-rule stats, episode coverage, or match rate history.
**Migration**: `GetMatchQuality` delegates to `MatchStatsActor`. Entity state has no analytics fields.

### Requirement: Persistence events
**Reason**: Reduced from 6 event types to 2. `MatchQualityRecorded`, `EpisodeMatched`, `MatchRateSnapshotRecorded` removed. `LocalOverrideApplied`/`LocalOverrideRemoved` merged into `LocalOverrideChanged`.
**Migration**: Entity persists only `RulesGenerated` and `LocalOverrideChanged`. Breaking persistence change (0.x).

### Requirement: Constructor dependencies
**Reason**: Moved to `SeriesRuleSetActor` with same constraint (IReadOnlyActorRegistry only).
**Migration**: Same pattern, new class name.

### Requirement: GetRuleSet message
**Reason**: Moved to `MediaRuleSetActor` base class.
**Migration**: Same behavior via base class.

### Requirement: TestRules message
**Reason**: Moved to `MediaRuleSetActor` base class.
**Migration**: Same behavior via base class.

### Requirement: RemoveLocalOverride message
**Reason**: Moved to `MediaRuleSetActor` base class.
**Migration**: Same behavior via base class. Event changed from `LocalOverrideRemoved` to `LocalOverrideChanged(null)`.

### Requirement: GetEpisodeCoverage message
**Reason**: Coverage data moved to `MatchStatsActor`.
**Migration**: Message routed to `MatchStatsActor` instead of entity.

### Requirement: GetMatchRateTrend message
**Reason**: Trend data moved to `MatchStatsActor`.
**Migration**: Message routed to `MatchStatsActor` instead of entity.
