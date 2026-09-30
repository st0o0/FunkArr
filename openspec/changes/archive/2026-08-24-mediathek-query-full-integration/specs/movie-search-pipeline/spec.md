## MODIFIED Requirements

### Requirement: Mediathek fetch stage

After movie resolution completes, MovieSearchActor SHALL Ask MediathekGateway with a `QueryItems` message containing a `MediathekSearchQuery.Search(title).WithDuration(min: 2400).ExcludeFuture().Limit(100).Build()` query using the resolved movie title.

#### Scenario: Mediathek query after resolution
- **WHEN** MovieResolver has responded with movie information
- **THEN** the entity SHALL Ask MediathekGateway with `QueryItems(MediathekSearchQuery.Search(title).WithDuration(min: 2400).ExcludeFuture().Limit(100).Build())`

### Requirement: Original title fallback

When a TMDB-resolved title yields no results from MediathekGateway, MovieSearchActor SHALL retry the mediathek query using `QueryItems(MediathekSearchQuery.Search(originalTitle).WithDuration(min: 2400).ExcludeFuture().Limit(100).Build())`.

#### Scenario: Fallback to original title
- **WHEN** the mediathek query using the TMDB-resolved title returns zero items and the movie has an `originalTitle` different from the resolved title
- **THEN** the entity SHALL retry with `QueryItems(MediathekSearchQuery.Search(originalTitle).WithDuration(min: 2400).ExcludeFuture().Limit(100).Build())`

#### Scenario: No fallback when titles match
- **WHEN** the mediathek query returns zero items but the `originalTitle` is identical to the resolved title
- **THEN** the entity SHALL NOT retry and SHALL return an empty result set
