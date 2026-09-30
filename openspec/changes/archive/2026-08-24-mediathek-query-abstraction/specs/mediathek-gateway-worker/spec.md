## MODIFIED Requirements

### Requirement: Throttled Mediathek access via Ask
`MediathekGatewayActor` SHALL respond to `QueryItems(MediathekSearchQuery Query)` with `ItemsQueried(MediathekResultItem[])`. It SHALL translate `MediathekSearchQuery` to the wire format `MediathekQuery` internally using `MediathekClient`. Rate limiting behavior SHALL be unchanged.

#### Scenario: Successful query
- **WHEN** `MediathekGatewayActor` receives `QueryItems(MediathekSearchQuery.ByTopic("Tatort").Build())`
- **THEN** it SHALL translate the query to wire format, call `MediathekClient`, and reply with `ItemsQueried` containing the results

#### Scenario: Mediathek API failure
- **WHEN** `MediathekClient` throws an `HttpRequestException`
- **THEN** `MediathekGatewayActor` SHALL reply with `ItemsQueried(Array.Empty<MediathekResultItem>())` and log a warning

## REMOVED Requirements

### Requirement: SearchMode enum
**Reason**: Replaced by `MediathekSearchQuery` builder entry points (`ByTopic`, `ByFullText`, `Latest`).
**Migration**: `SearchMode.Topic` → `MediathekSearchQuery.ByTopic(term)`, `SearchMode.FullText` → `MediathekSearchQuery.ByFullText(term)`.

## RENAMED Requirements

- FROM: `FetchItems` / TO: `QueryItems`
- FROM: `ItemsFetched` / TO: `ItemsQueried`
