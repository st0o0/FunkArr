## Context

The message layer in FunkArr.Messages uses `Ask<object>` for 11 of 13 Ask call sites because there are no shared response interfaces. Messages stack unrelated interfaces (`TvSearchCommand : ISearchCommand, IWithSearchId`), and the adapter layer (SearchHandler in ArrApi) builds domain-internal command types with dummy `Guid.Empty` SearchIds.

## Goals / Non-Goals

**Goals:**
- Eliminate all `Ask<object>` through domain-level response interfaces
- One unified SearchCommand as the public API entry point for all search types
- Clean interface hierarchy: each message implements at most one interface, or inherits through a clear chain
- Maintain ShardMessageExtractor compatibility via existing `IWith*Id` interfaces

**Non-Goals:**
- Changing the PipeTo pattern in workers (it's correct, only the type parameter changes)
- Modifying Persistence DTOs or actor state records
- Changing the shard message extractor logic
- Refactoring actors beyond adjusting Ask types and receiving SearchCommand

## Decisions

### One response interface per domain

Each domain gets a single marker interface for its response types:

```
ISearchResponse        ← SearchCompleted, SearchFailed
IRuleSetResponse       ← RuleSetResolved, RuleSetNotFound, RuleSetDetailResult, RegisteredRuleSetsResult
IMediathekResponse     ← MediathekQueryCompleted, MediathekQueryFailed
IScoringResponse       ← ScoreCompleted, ScoringHistoryResult, ScoringDetailResult, ScoringDetailNotFound
```

**Why per-domain, not per-operation:** Per-operation interfaces (`IResolveRuleSetResponse`, `IQueryRuleSetDetailResponse`) create naming bloat. The domain-level grouping is slightly broader than necessary — `Ask<IRuleSetResponse>` permits types that a given operation will never return — but the `Receive<ConcreteType>` handler catches the right one regardless. The type parameter is a safety net, not a guarantee.

**Alternative considered:** Union types / OneOf<T1,T2>. Rejected because Akka.NET's dispatch is runtime-typed — the compile-time union buys nothing and adds a dependency.

### Unified SearchCommand with ISearchParams interface

```csharp
public sealed record SearchCommand(
    string? Query,
    int? Cat,
    int? Limit,
    int? Offset,
    SearchCommand.ISearchParams? Params)
{
    public interface ISearchParams;
    public sealed record TvParams(int? Season, int? Episode, int? TvdbId, string? ImdbId) : ISearchParams;
    public sealed record MovieParams(string? ImdbId, int? TmdbId) : ISearchParams;
}
```

**Why a single Params property with interface:** Instead of two nullable properties (`Tv`, `Movie`), a single `ISearchParams? Params` makes the discriminator clean: `Params is TvParams` / `is MovieParams` / `null`. Adding a new search type later means adding one record implementing `ISearchParams`, not adding another nullable property.

**What happens to TvSearchCommand / MovieSearchCommand:** They stay in FunkArr.Messages (ShardMessageExtractor needs them) but are no longer public API. SearchManager creates them internally when routing to shard regions.

### Delete ISearchCommand and GeneralSearchCommand

`ISearchCommand` was a grouping marker for three commands. With a single `SearchCommand`, the marker is redundant. `GeneralSearchCommand` becomes `SearchCommand` with `Params=null`.

### RuleSetNotFound implements one interface

`RuleSetNotFound : IRuleSetResponse` — used as a response by both `ResolveRuleSet` and `QueryRuleSetDetail`. This is a true IS-A relationship, not interface stacking.

### TvSearchCommand / MovieSearchCommand implement only IWithSearchId

No more dual `ISearchCommand, IWithSearchId`. They only implement `IWithSearchId` for shard routing. Their role as "command types" is implicit — they're the messages that shard workers receive.

## Risks / Trade-offs

- **Broader-than-needed response types** → `Ask<IRuleSetResponse>` allows impossible combinations at compile time. Mitigated by runtime dispatch (`Receive<>`) catching the right type. The trade-off is acceptable because the alternative is naming hell.
- **SearchCommand carries fields irrelevant to some search types** (e.g. `Cat` only matters when `Params` is null) → This is deliberate. The SearchManager discriminates; the caller doesn't need to.
- **TvSearchCommand/MovieSearchCommand stay public** in FunkArr.Messages even though they're conceptually internal → Required because ShardMessageExtractor in FunkArr.Core references the `IWithSearchId` interface. Could be revisited if the extractor moves, but that's out of scope.
