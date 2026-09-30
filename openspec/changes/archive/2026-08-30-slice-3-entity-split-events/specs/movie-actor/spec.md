## REMOVED Requirements

### Requirement: MovieActor sharded persistent actor
**Reason**: Replaced by `MovieRuleSetActor` extending `MediaRuleSetActor`. See `media-ruleset-actor` spec.
**Migration**: All references to `MovieActor` SHALL be updated to `MovieRuleSetActor`. Shard region registration updated. PersistenceId changes from `"movie-{imdbId}"` to `"ruleset-movie-{imdbId}"`.

### Requirement: Movie identity resolution
**Reason**: Metadata caching removed from entity. Gateway is the single cache.
**Migration**: `ResolveMetadata` hook in `MovieRuleSetActor` asks `TmdbGatewayActor` on each request.

### Requirement: ResolveSearch message
**Reason**: Moved to `MovieRuleSetActor` subclass with `BuildSearchHint` hook.
**Migration**: `ResolveSearch` handled by `MovieRuleSetActor` via hook delegation.

### Requirement: Match message
**Reason**: Match pipeline moved to `MediaRuleSetActor` base class.
**Migration**: `MovieRuleSetActor.SelectStrategies` dispatches to `RuleSetMatchingEngine.EvaluateMovieRulesWithTraces`.

### Requirement: Match handler produces full traces
**Reason**: Trace emission moved to base class. Stats forwarding changed to `MatchStatsActor`.
**Migration**: Base class Tells `MatchStatsActor.RecordMatchRun`.

### Requirement: Ruleset ownership
**Reason**: Three-layer management moved to `MediaRuleSetActor` base class.
**Migration**: Same behavior, now in base class.

### Requirement: Match quality tracking
**Reason**: Analytics moved to `MatchStatsActor`.
**Migration**: Entity state has no analytics fields.

### Requirement: Persistence events
**Reason**: Reduced from 6 event types to 2. Breaking persistence change (0.x).
**Migration**: Entity persists only `RulesGenerated` and `LocalOverrideChanged`.

### Requirement: Constructor dependencies
**Reason**: Moved to `MovieRuleSetActor`.
**Migration**: Same pattern, new class name.

### Requirement: GetRuleSet message
**Reason**: Moved to `MediaRuleSetActor` base class.
**Migration**: Same behavior via base class.

### Requirement: TestRules message
**Reason**: Moved to `MediaRuleSetActor` base class.
**Migration**: Same behavior via base class.

### Requirement: RemoveLocalOverride message
**Reason**: Moved to base class.
**Migration**: Event changed from `LocalOverrideRemoved` to `LocalOverrideChanged(null)`.

### Requirement: GetEpisodeCoverage message
**Reason**: Coverage data moved to `MatchStatsActor`.
**Migration**: Message routed to `MatchStatsActor`.

### Requirement: GetMatchRateTrend message
**Reason**: Trend data moved to `MatchStatsActor`.
**Migration**: Message routed to `MatchStatsActor`.
