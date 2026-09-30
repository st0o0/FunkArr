## Context

ShowActor and MovieActor are near-identical persistent actors (~470 lines each, 11 messages) that manage rule-set layers, execute matching, cache metadata, and track analytics. Slices 0–2 established FunkArr.Core, FunkArr.Messages, per-family shard interfaces, and typed ids. This slice extracts the shared shape into an abstract base, moves analytics to a dedicated actor, and splits persistence per actor — following the approved design document §5 and §8.

Current state after slices 0–2:
- Shard interfaces (`ISeriesShard`, `IMovieShard`) with typed ids are in FunkArr.Messages
- Per-region extractors (`SeriesShardExtractor`, `MovieShardExtractor`) are in Configuration/Sharding
- Matching engine is split into FilterEvaluator + strategies in FunkArr.Core
- Events are nested records inside ShowActor/MovieActor (not in FunkArr.Messages)

## Goals / Non-Goals

**Goals:**
- One abstract base (`MediaRuleSetActor`) absorbing the repeated shape, with four closed hooks
- Adding a new media kind requires ~50 lines in one file plus one registration line
- No actor above ~200 lines or 8 message handlers
- Analytics separated from entity lifecycle into `MatchStatsActor`
- Entity persistence reduced to two event types: `RulesGenerated`, `LocalOverrideChanged`
- One journal file per actor — no shared DTOs between actors

**Non-Goals:**
- Rule-set merge/layering logic (Slice 4)
- Rule.Id assignment (Slice 4)
- Provider/failure model refactoring (Slice 5)
- Changing any external API contract

## Decisions

### D1: Abstract base class, not composition

**Choice:** `MediaRuleSetActor : ReceivePersistentActor` with four `protected abstract` hooks.

**Why over composition:** Akka.NET's `ReceivePersistentActor` lifecycle (recovery, persistence, passivation) is tightly coupled to the actor class. Composition would require delegating all lifecycle methods, which is more code than the duplication it removes. The base class pattern is proven in the reference project (njord's `StreamConsumerActor`).

**Hook contract (closed at four):**
1. `ResolveMetadata` — ask the appropriate gateway for metadata (episodes/movie info)
2. `BuildSearchHint` — construct `SearchHint` from resolved metadata + effective rules
3. `SelectStrategies` — dispatch to TV or movie matching in `RuleSetMatchingEngine`
4. `ComputeCoverage` — return episode coverage (series) or matched flag (movie)

A subclass needing a fifth hook signals the responsibility belongs elsewhere, not that the contract should widen.

### D2: MatchStatsActor as singleton, not sharded

**Choice:** Single `MatchStatsActor` with `PersistenceId = "match-stats"`, absorbing `RecentMatchActor`.

**Why not sharded per entity:** Analytics are queried across entities (fleet stats, unmatched-by-topic). A per-entity stats actor would require scatter-gather for every dashboard query. The singleton pattern matches `RecentMatchActor`'s current design and the append-heavy, cross-entity query pattern.

**Event model:** One event `MatchRunRecorded(MediaKey, RuleHitCounts, Matched, Unmatched, Filtered, MatchedEpisodes, At)`. MediaKey is the formatted shard key (`series-12345`, `movie-tt0133093`), so one actor family serves all media kinds.

**Projections:** Rule hit counts, episode coverage, and match-rate history are derived views over the event stream, maintained in-memory state. Snapshots every N events for recovery performance.

### D3: Entity events reduced to two

**Choice:** Entity persists only `RulesGenerated(RuleSetFile, Confidence, At)` and `LocalOverrideChanged(RuleSetFile?, At)`.

**Why:** `MatchQualityRecorded`, `EpisodeMatched`, and `MatchRateSnapshotRecorded` are analytics — different lifecycle, different query patterns. They move to `MatchStatsActor`. `LocalOverrideApplied` and `LocalOverrideRemoved` merge into `LocalOverrideChanged` (null payload = cleared). Community rules remain unpersisted — `RuleSetRegistryActor` pushes them from disk at startup.

**Breaking persistence:** Acceptable at 0.x per project policy. No migration code needed.

### D4: Metadata not cached in entity

**Choice:** Remove `_showName`, `_episodes`, `_movieTitle` from entity state. The entity asks the gateway on each match request.

**Why:** `TvdbGatewayActor` (24h TTL, persistent, request coalescing) and `TmdbGatewayActor` are already the cache. The entity copy is a second cache with no TTL that goes stale on actor passivation and requires recovery warmup. Removing it simplifies the entity and its journal.

**Trade-off:** Each match request now includes a gateway round-trip. The gateway's request coalescing means concurrent matches for the same entity share one API call, so the overhead is negligible.

### D5: Persistence IDs change

**Choice:**
- `show-{tvdbId}` → `ruleset-series-{tvdbId}` (SeriesRuleSetActor)
- `movie-{imdbId}` → `ruleset-movie-{imdbId}` (MovieRuleSetActor)
- `recent-match-actor` → `match-stats` (MatchStatsActor)

**Why:** The new IDs reflect the actor's role (ruleset management) rather than its media kind, and follow the `{domain}-{family}-{key}` pattern. Breaking change is acceptable at 0.x.

### D6: Messages stay in actor files, events move to FunkArr.Messages

**Choice:** Message records (commands, queries, responses) remain nested inside the actor class. Domain events for persisted actors move to dedicated `*Events.cs` files in FunkArr.Messages.

**Why:** Messages are the actor's public API and benefit from co-location. Events are the actor's persistence contract and need to be visible to persistence DTOs without depending on the actor assembly's Akka references.

### D7: Journal DTO split

**Choice:** One journal file per actor:
- `RuleSetActorJournal.cs` — shared by SeriesRuleSetActor and MovieRuleSetActor (same two event types)
- `MatchStatsJournal.cs` — MatchStatsActor DTOs
- Delete `ShowActorJournal.cs` (currently serving both Show and Movie via `ToMovieDomain()`)

**Why:** `ShowActorJournal.cs` currently has `Sh`-prefixed DTOs and a `ToMovieDomain()` overload to disambiguate — an encoding of the assumption that Show and Movie share a journal schema. With the entity events reduced to two and identical across media kinds, one shared `RuleSetActorJournal.cs` is appropriate. The prefix changes from `Sh` to `Rs`.

## Risks / Trade-offs

- **Base class could become a god class** → Mitigated by: no provider/IO in base, no actor > 200 lines / > 8 handlers, hook contract closed at 4. These are mechanically checkable.
- **MatchStatsActor singleton could become a bottleneck** → Mitigated by: it only receives fire-and-forget `Tell` messages from entities, no Ask. Recovery from snapshots bounds startup cost.
- **Gateway round-trip on every match** → Acceptable: gateway has request coalescing and 24h cache. The entity's passivation cycle (6h) meant the cached copy was often stale anyway.
- **Breaking persistence IDs** → Acceptable at 0.x. No migration needed per project policy.
