## Why

Search actors currently build raw `FetchItems(string, SearchMode)` messages to query the MediathekViewWeb API. This forces callers to understand the query semantics (which fields to search, result limits, sort order) and limits expressiveness — only two modes exist (FullText, Topic) while the API supports multi-field queries, channel filtering, pagination, and sorting. A domain-level query abstraction with a builder pattern lets search actors express rich intent without coupling to the wire format.

## What Changes

- Introduce `MediathekSearchQuery` — an immutable record with a builder pattern that provides a fluent API for constructing Mediathek queries. Static factory entry points (`ByTopic`, `ByFullText`) enforce that every query starts with a search criterion. Chainable methods (`.FromChannel()`, `.WithTitle()`, `.Limit()`, `.SortByNewest()`) compose constraints. `.Build()` validates and produces the immutable record.
- **BREAKING**: Rename `FetchItems` → `QueryItems` and `ItemsFetched` → `ItemsQueried` across the actor protocol. `QueryItems` wraps a `MediathekSearchQuery` instead of a raw string + enum.
- **BREAKING**: Remove `SearchMode` enum — replaced by the query builder's entry points.
- `MediathekGatewayActor` translates `MediathekSearchQuery` → `MediathekQuery` (wire format) internally. No changes to `MediathekClient` or the HTTP layer.
- Update all search actors (`TvSearchActor`, `MovieSearchActor`, `TextSearchActor`) to use the builder API.

## Capabilities

### New Capabilities
- `mediathek-query-builder`: Fluent builder API for constructing typed MediathekViewWeb queries. Covers the `MediathekSearchQuery` record, builder pattern, field mapping semantics, validation rules, and translation to wire format.

### Modified Capabilities
- `mediathek-gateway-worker`: Message protocol changes from `FetchItems`/`ItemsFetched` to `QueryItems`/`ItemsQueried`. The gateway now receives `MediathekSearchQuery` and translates to wire format internally. Rate limiting and stateless behavior unchanged.
- `tv-search-pipeline`: Adopts `MediathekSearchQuery.ByTopic()` instead of `FetchItems(term, SearchMode.Topic)`.
- `movie-search-pipeline`: Adopts `MediathekSearchQuery.ByFullText()` instead of `FetchItems(title, SearchMode.FullText)`.
- `text-search-pipeline`: Adopts `MediathekSearchQuery.ByFullText()` instead of `FetchItems(query, SearchMode.FullText)`.

## Impact

- **Code**: `SearchCoordinatorMessages.cs` (message renames + new query type), `MediathekGatewayActor.cs` (query translation), `TvSearchActor.cs`, `MovieSearchActor.cs`, `TextSearchActor.cs` (builder adoption).
- **Tests**: All tests referencing `FetchItems`/`ItemsFetched`/`SearchMode` need updating. New unit tests for builder validation and query-to-wire translation.
- **APIs**: No external API changes — this is internal actor protocol only.
- **Dependencies**: None — pure refactor with new abstraction.
