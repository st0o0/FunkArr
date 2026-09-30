## REMOVED Requirements

### Requirement: Periodic RSS feed refresh
**Reason**: Replaced by TextSearchPipeline empty-query caching. MediathekViewWeb returns the latest content when called with an empty queries array, eliminating the need for a dedicated actor with its own timer, scatter-gather, and RuleSetCoordinator dependency.
**Migration**: Empty Newznab search requests now flow through `SearchRouter` → `TextSearchPipeline("")`. The TextSearchPipeline's 55-minute cache provides equivalent freshness. No configuration migration needed — `RssFeedOptions` settings are removed.
