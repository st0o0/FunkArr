## Context

FunkArr currently uses a single `IShardedMessage { string EntityKey }` interface and one `ShardedMessageExtractor` for all 5 shard regions. Entity actors parse their identity from `Self.Path.Name` at runtime. This provides no compile-time routing safety and scatters id-parsing across actor constructors.

After Slice 1, `FunkArr.Messages` exists but only holds event files — the message files that implement `IShardedMessage` couldn't move. This slice replaces the generic interface, enabling the remaining message moves.

## Goals / Non-Goals

**Goals:**
- One typed shard interface per entity family, in FunkArr.Messages
- Per-region extractors that read the typed Id (in FunkArr, since they depend on Akka)
- Entity actors receive their typed id in the constructor
- `IRequest<TResponse>` marker interface for typed ask patterns
- Move remaining message files to FunkArr.Messages
- Delete `IShardedMessage`, `ShardedMessageExtractor`, `IWithNzoId`

**Non-Goals:**
- Changing NzoId from string to Guid (the design doc shows `Guid Id` but the current codebase uses string throughout the download pipeline — changing it is a separate concern)
- Moving nested actor messages out of actor files (Slice 3)
- Actor gateway `IActorGateway` (Slice 6)

## Decisions

1. **Shard interfaces use the actual id type, not wrappers.** Per the design doc §7 "no wrapper types" rule:
   ```csharp
   public interface ISeriesShard   { int Id { get; } }
   public interface IMovieShard    { string Id { get; } }
   public interface IDownloadShard { string Id { get; } }
   ```
   `IDownloadShard.Id` stays `string` (not `Guid`) because NzoId is string throughout the download pipeline. Changing it is a separate concern.

2. **Search messages get `ISeriesShard`, `IMovieShard`, or a dedicated `ISearchShard` based on their actual routing.** `SearchRequest.Tv` targets the search-request region with `$"tv:{TvdbId}"`. Since the search region uses string keys with type prefixes, search messages keep a string-based approach: `ISearchShard { string Id }`.

3. **Per-region extractors format readable shard keys.** `SeriesShardExtractor` formats `$"series-{id}"`, `MovieShardExtractor` formats `$"movie-{id}"`, etc. The shard key is what appears in logs, shard-stats dumps, and journal rows.

4. **Entity actors receive id via entityPropsFactory.** The actor system setup passes the entity id string to a factory that creates the Props with the typed id:
   ```csharp
   entityPropsFactory: (_, _, resolver) => entityId => resolver.Props<ShowActor>(int.Parse(entityId))
   ```
   The parse happens once in the factory. The actor constructor takes the typed id directly.

5. **IRequest<TResponse> is a marker interface only.** No runtime behavior — it enables arch tests (§10 rule 10) and future typed ask helpers:
   ```csharp
   public interface IRequest<TResponse>;
   ```

6. **RuleSetRegistryActor's EntityKey stays as-is.** Its messages use `EntityKey` as a plain string property (not `IShardedMessage`). It's not a sharded actor — no changes needed there.

7. **Transition order:** Add new interfaces first (non-breaking), update messages and actors, update extractors and setup, then delete old types. Build stays green at each step.

## Risks / Trade-offs

- **Risk: SearchRequestActor routes on string prefix today.** The current `msg.EntityKey.StartsWith("tv:")` pattern needs replacement. → Mitigation: SearchRequestActor can pattern-match on the message type (`SearchRequest.Tv`, `.Movie`, `.Text`) instead of string-prefix routing.
- **Risk: 34 records change simultaneously.** → Mitigation: Records keep the same constructors and fields. Only the interface implementation changes — `IShardedMessage` → `ISeriesShard` etc. The `EntityKey` property is removed.
