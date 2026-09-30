## MODIFIED Requirements

### Requirement: Mediathek fetch stage

After parallel resolution completes, TvSearchActor SHALL Ask MediathekGateway with a `QueryItems` message containing a `MediathekSearchQuery.ByTopic(searchTerm).ExcludeFuture().Limit(200).Build()` query using the resolved show information. When the RuleSet provides channel information, the query SHALL additionally call `.FromChannel(channel)`.

#### Scenario: Mediathek query after resolution
- **WHEN** both SeriesResolver and RuleSetActor have responded
- **THEN** the entity SHALL Ask MediathekGateway with `QueryItems(MediathekSearchQuery.ByTopic(searchTerm).ExcludeFuture().Limit(200).Build())`

#### Scenario: Mediathek query with channel from RuleSet
- **WHEN** both SeriesResolver and RuleSetActor have responded and the RuleSet provides a channel
- **THEN** the entity SHALL Ask MediathekGateway with `QueryItems(MediathekSearchQuery.ByTopic(searchTerm).FromChannel(channel).ExcludeFuture().Limit(200).Build())`

#### Scenario: Mediathek query without channel
- **WHEN** both SeriesResolver and RuleSetActor have responded and the RuleSet does not provide a channel
- **THEN** the entity SHALL omit `.FromChannel()` from the query
