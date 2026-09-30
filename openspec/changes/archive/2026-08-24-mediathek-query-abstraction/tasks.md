## 1. Query Abstraction

- [x] 1.1 Create `SortField` and `SortDirection` enums in `Search/MediathekSearchQuery.cs`
- [x] 1.2 Create `MediathekSearchQuery` sealed record with properties: `Topic`, `Title`, `Channel`, `FullText`, `MaxResults`, `SortBy`, `SortDir`
- [x] 1.3 Implement `QueryBuilder` nested class with static factory entry points (`ByTopic`, `ByFullText`, `Latest`) and chainable methods (`WithTitle`, `FromChannel`, `Limit`, `SortByNewest`)
- [x] 1.4 Implement `Build()` validation: mutual exclusion of Topic/FullText, MaxResults > 0, at least one criterion unless Latest
- [x] 1.5 Write unit tests for builder: valid constructions, chainable methods, validation failures

## 2. Message Protocol Rename

- [x] 2.1 Rename `FetchItems` → `QueryItems(MediathekSearchQuery Query)` in `SearchCoordinatorMessages.cs`
- [x] 2.2 Rename `ItemsFetched` → `ItemsQueried` in `SearchCoordinatorMessages.cs`
- [x] 2.3 Remove `SearchMode` enum from `SearchCoordinatorMessages.cs`

## 3. Gateway Translation

- [x] 3.1 Add wire format translation method in `MediathekGatewayActor` that maps `MediathekSearchQuery` → `MediathekQuery`
- [x] 3.2 Update `MediathekGatewayActor` to receive `QueryItems` instead of `FetchItems` and use translation method
- [x] 3.3 Write unit tests for query-to-wire translation: topic-only, full-text, combined fields, latest, custom limit/sort

## 4. Search Actor Migration

- [x] 4.1 Update `TvSearchActor` to use `QueryItems(MediathekSearchQuery.ByTopic(...).Build())`
- [x] 4.2 Update `MovieSearchActor` to use `QueryItems(MediathekSearchQuery.ByFullText(...).Build())` including original title fallback
- [x] 4.3 Update `TextSearchActor` to use `QueryItems(MediathekSearchQuery.ByFullText(...).Build())` for queries and `MediathekSearchQuery.Latest().Build()` for empty queries

## 5. Test Updates

- [x] 5.1 Update all test files referencing `FetchItems`/`ItemsFetched`/`SearchMode` to new names
- [x] 5.2 Run full test suite and fix any remaining compilation errors
- [x] 5.3 Run `dotnet format` on all modified files
