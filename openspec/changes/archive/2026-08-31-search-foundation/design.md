## Context

FunkArr's ArrApi (Newznab + SABnzbd) is fully implemented but returns stub responses. MatchMagic has a complete ruleset evaluation engine (pure logic). The search pipeline that connects incoming Prowlarr requests to MediathekViewWeb queries and scored results does not exist yet.

MediathekViewWeb API: `POST https://mediathekviewweb.de/api/query` with `Content-Type: text/plain`. Supports field-based queries (title, topic, channel, description), duration filters, sorting, and pagination. Returns items with three quality variants (low/normal/HD), subtitles, and website links. No authentication, no documented rate limits.

Prowlarr uses three Newznab search types: `t=tvsearch` (season/ep/tvdbid), `t=movie` (imdbid/tmdbid), `t=search` (general, routed by `cat` parameter: 5xxx=TV, 2xxx=Movie).

## Goals / Non-Goals

**Goals:**
- Functional search pipeline: Prowlarr search request → MediathekViewWeb query → MatchMagic scoring → Newznab RSS response
- TV and Movie search as independent sharded actor subsystems with identical communication patterns
- Centralized, rate-limited access to MediathekViewWeb via singleton actor
- MatchMagic as actor with loaded RuleSet state for scoring requests
- All messages as primitive records in FunkArr.Messages

**Non-Goals:**
- Persistence of search results (ephemeral, no event sourcing)
- Download pipeline (Phase 2+)
- UI or internal API endpoints (Phase 4)
- RuleSet management/loading from external sources (Phase 2)
- Caching of MediathekViewWeb responses
- Multi-node cluster distribution

## Decisions

### 1. Actor topology: Gateway → Sharded Workers → Singletons

```
ArrApi ──Ask──▶ SearchGatewayManager (Singleton)
                    │
         ┌──────────┴──────────┐
         ▼                     ▼
  TvSearchWorker          MovieSearchWorker
  (Sharded, SearchId)     (Sharded, SearchId)
         │                     │
         ├──Ask──▶ MediathekViewWebManager (Singleton)
         │              stashing, max N concurrent HTTP
         │
         └──Ask──▶ MatchMagicManager (Singleton)
                        holds RuleSets, pure scoring
```

**Why sharded workers with SearchId?** Each search is an independent unit of work. Sharding by a per-request Guid gives natural parallelism — multiple searches run concurrently without contention. Workers are transient: process one search, respond, passivate.

**Alternative considered:** Router pool pattern. Rejected because sharding provides the same parallelism with cluster-ready semantics and consistent message routing via the shard region.

### 2. Gateway manages sender correlation internally

The SearchGatewayManager captures `Sender` on each incoming Ask and stores it in a `Dictionary<SearchId, PendingSearch>` state. Workers respond to `Sender` (which is the Gateway), and the Gateway forwards to the captured original sender.

**Why not replyTo in messages?** IActorRef is not serializable in messages. Keeping correlation in the Gateway keeps messages as pure primitive records and gives the Gateway full visibility over in-flight searches (observability).

**Fan-out for `t=search` without `cat`:** Gateway sends to both shard regions with the same SearchId, tracks two pending results in PendingSearch state, merges when both arrive (or responds with partial results on timeout).

```
sealed record PendingSearch(
    IActorRef OriginalSender,
    SearchType Type,           // Tv, Movie, Both
    SearchResult? TvResult,
    SearchResult? MovieResult
);
```

### 3. MediathekViewWebManager: stashing-based backpressure

The singleton accepts up to N concurrent HTTP requests (configurable, default 3). Requests beyond N are stashed. When a response arrives, the next stashed request is unstashed.

**Why stashing over work-pulling?** Simpler protocol — no separate "give me work" handshake. Stash overflow risk is acceptable because search volume is bounded by Prowlarr's request rate (low).

**MediathekQueryBuilder** is an internal class (not an actor) within the manager. It translates `MediathekQuery` messages to the API's JSON format. Pure 1:1 mapping of all API features — no search strategy logic.

### 4. MatchMagicManager: actor wrapper around pure logic

The existing MatchMagic library (RuleSet, Rule, Filter, etc.) stays pure — no Akka dependency. MatchMagicManager is a singleton actor that:
- Holds loaded RuleSets in `Dictionary<string, RuleSet>` state
- Accepts `ScoreItems` requests, evaluates using the pure library, responds with `ScoreCompleted`
- Accepts `LoadRuleSet` / `UnloadRuleSet` for state management

**Why an actor instead of DI service?** Consistent message-driven architecture. RuleSet state is mutable (load/unload), and an actor serializes access naturally.

### 5. Worker orchestration via PipeTo

Search workers chain two Ask calls (Mediathek → MatchMagic) using Akka's PipeTo pattern to avoid blocking the actor's thread:

```
Receive<TvSearchCommand> → Ask MediathekViewWebManager → PipeTo(Self)
Receive<MediathekQueryCompleted> → Ask MatchMagicManager → PipeTo(Self)  
Receive<ScoreCompleted> → build SearchCompleted → Tell Gateway
```

### 6. All quality variants modeled

MediathekItem includes all three URL variants from the API plus subtitle:
- `url_video_low` (~480x270)
- `url_video` (~960x540, normal)
- `url_video_hd` (~1280x720)
- `url_subtitle` (XML/SRT)
- `url_website` (browser link)

Quality selection happens downstream (download phase), not during search.

## Risks / Trade-offs

- **[MediathekViewWeb availability]** → Single external dependency with no fallback. Mitigation: stashing naturally queues during outages; workers timeout and respond with SearchFailed.
- **[No rate limit documentation]** → Could get blocked if too aggressive. Mitigation: configurable concurrent request cap in MediathekViewWebManager (default 3), easy to adjust.
- **[Sharding overhead for transient workers]** → Shard region bookkeeping for short-lived entities. Mitigation: acceptable for search volumes; passivation timeout keeps shard count bounded.
- **[MatchMagic singleton bottleneck]** → All scoring goes through one actor. Mitigation: scoring is CPU-only and fast (sub-millisecond per item); not a bottleneck at expected volumes.
