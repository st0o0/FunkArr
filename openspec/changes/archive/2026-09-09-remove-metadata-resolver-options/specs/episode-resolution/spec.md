## REMOVED Requirements

### Requirement: ResolutionConfig controls strategy behavior
**Reason**: Per-RuleSet resolution configuration is unused. The `ResolutionConfig` record defaults are always used. The config record itself remains as the message contract for `ResolveEpisodes`, but it is no longer configurable per-RuleSet.
**Migration**: No migration needed. `ResolutionConfig` record continues to exist with the same defaults. Callers use `new ResolutionConfig()` directly.
