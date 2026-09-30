## Context

ShowActor and MovieActor each handle three concerns: external API resolution (TVDB/TMDB), ruleset management, and match quality tracking. This creates oversized actors (~430 LOC each), fat journals (full RuleSetFile documents in events), and uncontrolled external API fan-out (each sharded entity calls TVDB/TMDB independently).

The existing `MediathekGatewayActor` already demonstrates the singleton-gateway pattern for MediathekViewWeb: queue-based rate limiting, single inflight request, drain timer. The new gateway actors follow this proven pattern.

Community rulesets are loaded from `data/community/rulesets/*.json` by `RuleSetRegistryActor` and pushed to media actors on every startup via `PushToMediaActors()`. The ShowActor/MovieActor then persist these as `CommunityRulesApplied` events — a redundant copy, since the next startup re-pushes from disk anyway.

## Goals / Non-Goals

**Goals:**
- Extract TVDB/TMDB API access into dedicated singleton gateway actors with event-sourced caching
- Eliminate redundant `CommunityRulesApplied` and `ShowResolved`/`MovieResolved` events from ShowActor/MovieActor journals
- Centralize external API rate limiting through actor mailbox serialization
- Add request deduplication in gateways (multiple callers for same entity = one API call)
- Add hash-based diffing to RuleSetRegistryActor so only changed rulesets are journaled and pushed
- Remove snapshots from ShowActor, MovieActor, and RuleSetRegistryActor (event volume is low enough)

**Non-Goals:**
- Changing the community ruleset JSON schema (future change, enabled by this extraction)
- Adding proactive background refresh for cached gateway data (TTL on read is sufficient)
- Changing the SearchRequestActor pipeline or the SearchHint/MatchedResults protocols
- Moving local overrides out of ShowActor/MovieActor (possible future simplification)

## Decisions

### Decision 1: Singleton gateway actors, not sharded

**Choice**: Single `TvdbGatewayActor` and single `TmdbGatewayActor`, registered via Akka.Hosting.

**Why not sharded by tvdbId/imdbId**: TVDB and TMDB have API-key-level rate limits, not per-entity limits. A singleton naturally serializes access through its mailbox. Sharding would create thousands of tiny journal streams and make cross-entity rate limiting difficult.

**Why not stateless service**: Event-sourced state survives restarts — after recovery, all previously cached shows/movies are immediately available without API calls. A stateless service with `IMemoryCache` loses everything on restart.

**Trade-off**: All TVDB traffic flows through one mailbox. At FunkArr's scale (60-100 shows, searches triggered by Sonarr), this is negligible. The MediathekGatewayActor already proves this pattern at higher volume.

### Decision 2: Request deduplication with waiter lists

**Choice**: Gateway actors maintain `Dictionary<int, List<IActorRef>>` for pending requests. If a second request arrives for the same tvdbId while the first is inflight, the sender is added to the waiter list. When the API response arrives, all waiters are notified.

**Why**: On startup, `RuleSetRegistryActor` pushes community rules to 60 ShowActors. If those ShowActors simultaneously ask the gateway for metadata, deduplication prevents 60 redundant API calls.

**Alternative considered**: Queue all requests and process sequentially (current MediathekGatewayActor pattern). Rejected because gateway requests are keyed — sequential processing would delay unrelated requests behind redundant ones.

### Decision 3: Community rules transient in ShowActor/MovieActor

**Choice**: `CommunityRulesApplied` is no longer a persisted event. Community rules are held in RAM and re-pushed by `RuleSetRegistryActor` on every startup.

**Why**: The registry already calls `PushToMediaActors()` on recovery. The `CommunityRulesApplied` events in each ShowActor journal are purely redundant copies. Removing them eliminates the largest events from the journal (each contains a full `RuleSetFile` with all rules).

**What stays persisted**: `RulesGenerated` (runtime-created, not on disk), `LocalOverrideApplied`/`Removed` (user-defined, not re-pushable), `MatchQualityRecorded` (stats).

**Risk**: If the registry hasn't finished recovery/push when a ShowActor receives a search request, the ShowActor won't have community rules yet. Mitigation: ShowActor already handles this case — it falls back to auto-generation when no rules exist. Additionally, the registry push happens during actor system startup before external traffic arrives.

### Decision 4: Hash-based diffing in RuleSetRegistryActor

**Choice**: On community refresh (disk reload or GitHub release update), compute SHA256 hash of each JSON file content. Compare against the last known hash per entity key. Only persist `RuleSetLoaded`/`RuleSetUpdated` events for changed files. Detect removed files and persist `RuleSetRemoved`.

**Why**: Current approach clears and reloads everything on refresh, pushing all 60 rulesets to all ShowActors regardless of changes. With hash-based diffing, a typical refresh (1-2 changed files) produces 1-2 events and 1-2 push messages instead of 60.

**Event content**: The full `RuleSetFile` is stored in the event (not just a reference). At ~1.5KB per ruleset and ~60 rulesets, total journal size is ~90KB — trivial. This keeps the journal self-contained: recovery replays events without needing disk access.

### Decision 5: No snapshots anywhere

**Choice**: Remove snapshot logic from ShowActor, MovieActor, and RuleSetRegistryActor. Gateway actors also have no snapshots.

**Why**:
- TvdbGatewayActor: ~4 events per show × 60 shows = 240 initial. With weekly TTL refreshes: ~3000 events/year. Recovery < 500ms after 5 years.
- TmdbGatewayActor: ~1 event per movie × 10-20 movies. Negligible.
- RuleSetRegistryActor: 60 initial loads + ~60 updates/year. Recovery < 100ms after 5 years.
- ShowActor: Only `RulesGenerated` + `MatchQualityRecorded` + local overrides. Typically < 50 events per show. Recovery < 10ms.
- MovieActor: Same, even fewer events.

Snapshots add complexity (snapshot classes, save/delete logic, recovery branching). At these volumes, they provide no measurable benefit.

### Decision 6: TTL as read-time decision, not journal events

**Choice**: Gateway actors don't persist expiry events. Instead, cached entries carry a `CachedAtUtcTicks` field. On read, the actor checks `(now - cachedAt) > TTL`. If expired, it fetches from the API and persists a new `ShowInfoCached`/`MovieInfoCached` event that overwrites the old entry in state.

**Why**: Expiry events would bloat the journal with no informational value. The state `Apply` method uses dictionary assignment (`_shows[tvdbId] = ...`), so a newer event naturally supersedes an older one during replay.

**TTL values**: 24 hours for both TVDB and TMDB, matching the current TTL in ShowActorState/MovieActorState.

### Decision 7: ShowActor asks gateway via Ask pattern

**Choice**: When `ShowActor` handles `ResolveSearch` and needs TVDB data, it uses `Ask<ShowResolved>(gateway, ResolveShow(...))` with PipeTo.

**Why**: The ShowActor needs the gateway response before it can reply with a `SearchHint`. Ask+PipeTo is the standard Akka.NET pattern for request-response between actors.

**Transient caching**: ShowActor keeps the gateway response in non-persisted fields (`_showName`, `_episodes`, `_gatewayFetchedAt`). Subsequent requests reuse this RAM cache until passivation (6h). On re-activation, the actor asks the gateway again (which serves from its journaled cache instantly).

## Risks / Trade-offs

**[Gateway singleton bottleneck]** → All TVDB/TMDB traffic serializes through one actor. At FunkArr's scale (tens of shows, searches triggered by Sonarr every ~15 minutes), this is not a concern. The MediathekGatewayActor handles higher volume with the same pattern. If scale ever matters, the gateway can be sharded later without changing the protocol.

**[Startup ordering dependency]** → ShowActor needs gateway + registry to be ready before processing searches. Mitigation: Akka.Hosting registration order ensures gateways and registry are started first. The registry already has a startup push. The sharded ShowActors only activate when a search arrives (after startup is complete).

**[Journal growth over years]** → Gateway journals grow indefinitely without snapshots. At ~3000 events/year for TVDB, this reaches ~15000 events after 5 years. Recovery time is still under 500ms. If this ever becomes an issue, snapshots can be added without changing the event model.

**[Breaking persistence change]** → ShowActor and MovieActor lose existing events (`ShowResolved`, `CommunityRulesApplied`, `MovieResolved`). Existing journals become incompatible. Per project convention (v0.x), this is acceptable — users wipe persistence on upgrade. No migration tooling needed.

**[Community rules unavailable during startup window]** → Between ShowActor recovery and RuleSetRegistryActor push completion, a ShowActor may lack community rules. This window is typically < 1 second. The ShowActor already handles missing rules gracefully (auto-generation fallback). In practice, Sonarr doesn't send searches during FunkArr startup.
