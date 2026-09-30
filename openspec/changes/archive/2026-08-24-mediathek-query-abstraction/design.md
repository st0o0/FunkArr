## Context

Search actors (`TvSearchActor`, `MovieSearchActor`, `TextSearchActor`) communicate with `MediathekGatewayActor` via `FetchItems(string SearchTerm, SearchMode Mode)`. This flat message forces callers to know that `SearchMode.Topic` maps to `fields: ["topic"]` and `SearchMode.FullText` to `fields: ["topic", "title"]`. The MediathekViewWeb API supports richer queries (multi-field, channel filtering, pagination, sorting) that are currently inaccessible.

The wire format types (`MediathekQuery`, `MediathekQueryItem`) live in `MediathekClient.cs` and are currently visible to the gateway actor. Search actors don't reference them directly but are coupled to the same abstraction level through `SearchMode`.

## Goals / Non-Goals

**Goals:**
- Domain-level query abstraction with builder pattern for expressive, self-documenting query construction
- Clean separation: search actors express intent, gateway translates to wire format
- Make `SearchMode` enum obsolete — builder entry points replace it
- Rename actor protocol messages to `QueryItems`/`ItemsQueried` for consistency with query semantics
- Extensible for future capabilities (channel filtering, pagination) without protocol changes

**Non-Goals:**
- Changing `MediathekClient` or the HTTP wire format (`MediathekQuery`, `MediathekQueryItem`)
- Changing the gateway's rate-limiting/queueing behavior
- Adding new query capabilities in this change (channel filtering, pagination are enabled but not wired into search actors yet)
- Modifying result types (`ItemsQueried` still returns `MediathekResultItem[]`)

## Decisions

### Decision: Builder pattern with static factory entry points

`MediathekSearchQuery` is an immutable sealed record. Construction goes through static factory methods that return a `QueryBuilder`. The builder validates on `.Build()`.

```
MediathekSearchQuery.ByTopic("Tatort").Limit(1000).Build()
MediathekSearchQuery.ByFullText("Der Untergang").Build()
MediathekSearchQuery.ByTopic("Tatort").WithTitle("Gefangen").SortByNewest().Build()
```

**Why static factories over `new QueryBuilder()`**: Forces every query to start with a search criterion. You cannot construct a meaningless empty query. The entry point name (`ByTopic`, `ByFullText`) documents the intent at the callsite.

**Why not a constructor with optional parameters**: Optional parameters don't guide the caller toward valid combinations. `ByTopic` vs `ByFullText` makes the mutual exclusion obvious — a constructor with both `topic` and `fullText` parameters invites confusion.

**Alternatives considered:**
- *Separate message types per query shape* (`SearchByTopic`, `SearchByTitle`, ...): Combinatorial explosion with each new filter dimension. Rejected.
- *Single record with nullable fields, no builder*: Valid but loses the guided construction and validation-at-build-time. The builder adds minimal code for significant API clarity.

### Decision: Query-to-wire translation lives in MediathekGatewayActor

The gateway actor contains a private `ToWireQuery` method that maps `MediathekSearchQuery` → `MediathekQuery`. This keeps the wire format types internal to the gateway + client layer.

**Why not in MediathekSearchQuery itself**: The query record is a domain concept. It shouldn't know about `MediathekQueryItem` or JSON field names. The gateway is the boundary where domain → wire translation belongs.

**Why not in MediathekClient**: The client is a thin HTTP wrapper. Adding domain awareness would couple it to the query abstraction.

### Decision: Rename FetchItems/ItemsFetched → QueryItems/ItemsQueried

Follows the existing `Verb+Noun` → `Noun+Verbed` pattern (`MatchItems`/`ItemsMatched`, `ScoreResults`/`ResultsScored`). The rename reflects that the message now carries a structured query, not a raw fetch instruction.

`QueryItems` wraps a `MediathekSearchQuery`. `ItemsQueried` stays structurally identical to `ItemsFetched` (contains `MediathekResultItem[]`).

### Decision: SearchMode enum removed

`SearchMode.Topic` → `MediathekSearchQuery.ByTopic(term)`
`SearchMode.FullText` → `MediathekSearchQuery.ByFullText(term)`

No migration needed — version is 0.x, clean breaks are fine.

### Decision: Empty query for RSS/browse

`TextSearchActor` currently sends `FetchItems("", FullText)` for RSS feeds (empty query = latest content). The builder handles this via `MediathekSearchQuery.Latest()` — a dedicated factory that produces a query with no search terms and a small result limit.

## Risks / Trade-offs

- **[Risk] Builder validation gaps** → Validate in `Build()`: at least one search criterion set, `ByTopic` and `ByFullText` mutually exclusive, `Limit` > 0. Throw `ArgumentException` on invalid combinations — fail fast at construction, not at query time.
- **[Risk] Rename churn across tests** → Mechanical find-replace. All tests referencing `FetchItems`/`ItemsFetched`/`SearchMode` need updating. No logic changes in tests, just type names.
- **[Trade-off] Builder adds a class** → Minimal overhead (~30 lines). The callsite clarity justifies it — compare `new FetchItems("Tatort", SearchMode.Topic)` vs `MediathekSearchQuery.ByTopic("Tatort").Build()`.

## Open Questions

None — design is straightforward and all decisions were explored in the discovery session.
