## MODIFIED Requirements

### Requirement: Throttled Mediathek access via Ask

`MediathekGatewayActor` SHALL respond to `QueryItems(MediathekSearchQuery Query)` with `ItemsQueried(MediathekResultItem[])`. It SHALL translate `MediathekSearchQuery` to the wire format `MediathekQuery` internally using `MediathekClient`, including the new fields: `operator` per query item, `duration_min`, `duration_max`, and `future` at the top level. Rate limiting behavior SHALL be unchanged.

#### Scenario: Successful query
- **WHEN** `MediathekGatewayActor` receives `QueryItems(MediathekSearchQuery.ByTopic("Tatort").Build())`
- **THEN** it SHALL translate the query to wire format (including operator field), call `MediathekClient`, and reply with `ItemsQueried` containing the results

#### Scenario: Query with duration and future filters
- **WHEN** `MediathekGatewayActor` receives a `QueryItems` with `DurationMin = 2400` and `ExcludeFuture = true`
- **THEN** the wire format SHALL include `duration_min: 2400` and `future: false`, and the actor SHALL reply with `ItemsQueried` containing only matching results

#### Scenario: Mediathek API failure
- **WHEN** `MediathekClient` throws an `HttpRequestException`
- **THEN** `MediathekGatewayActor` SHALL reply with `ItemsQueried(Array.Empty<MediathekResultItem>())` and log a warning
