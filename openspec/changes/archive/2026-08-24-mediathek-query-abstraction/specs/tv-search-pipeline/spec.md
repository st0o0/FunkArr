## MODIFIED Requirements

### Requirement: Mediathek fetch stage

After parallel resolution completes, TvSearchActor SHALL Ask MediathekGateway with a `QueryItems` message containing a `MediathekSearchQuery.ByTopic(searchTerm).Build()` query using the resolved show information.

#### Scenario: Mediathek query after resolution
- **WHEN** both SeriesResolver and RuleSetActor have responded
- **THEN** the entity SHALL Ask MediathekGateway with `QueryItems(MediathekSearchQuery.ByTopic(searchTerm).Build())`
