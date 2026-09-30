## MODIFIED Requirements

### Requirement: Topic-specific querying for TV search

The MediathekGateway SHALL support a search mode parameter on `FetchItems` that controls which Mediathek fields are queried. When `SearchMode` is `Topic`, the query SHALL use `fields: ["topic"]` to search only the topic field. When `SearchMode` is `FullText` (default), the query SHALL use `fields: ["topic", "title"]` as current behavior.

#### Scenario: Topic-only search
- **WHEN** `FetchItems` is received with `SearchTerm = "Tatort"` and `SearchMode = Topic`
- **THEN** the Mediathek query SHALL use `fields: ["topic"]` and `query: "Tatort"`

#### Scenario: Full-text search (default)
- **WHEN** `FetchItems` is received with `SearchTerm = "Tatort"` and `SearchMode = FullText`
- **THEN** the Mediathek query SHALL use `fields: ["topic", "title"]` and `query: "Tatort"`

#### Scenario: Backward compatibility
- **WHEN** `FetchItems` is received without an explicit `SearchMode`
- **THEN** the gateway SHALL default to `FullText` behavior

#### Scenario: Blank search term unchanged
- **WHEN** `FetchItems` is received with an empty or null `SearchTerm`
- **THEN** the gateway SHALL use `match_all` with `size: 100` regardless of `SearchMode`
