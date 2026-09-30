## 1. TvdbGatewayActor

- [x] 1.1 Create `TvdbGatewayActor` as `ReceivePersistentActor` with PersistenceId `"tvdb-gateway"` — events: `ShowInfoCached`, `EpisodesCached`; state: `Dictionary<int, CachedShowInfo>` with per-entry `CachedAtUtcTicks`; no snapshots
- [x] 1.2 Create `TvdbGatewayActorState` with `Apply` methods for both event types (dictionary assignment semantics — newer event overwrites older for same key); `IsExpired(tvdbId)` method using 24h TTL against `CachedAtUtcTicks`
- [x] 1.3 Implement `GetShowInfo`/`ShowInfoResponse` handler — check cache freshness, if expired or missing call `TvdbClient.GetShowAsync` via PipeTo, persist `ShowInfoCached`, reply to sender
- [x] 1.4 Implement `GetEpisodes`/`EpisodesResponse` handler — same pattern, per (tvdbId, season) cache key, calls `TvdbClient.GetEpisodesAsync`
- [x] 1.5 Implement `ResolveShow`/`ShowResolved` convenience handler — combines show info + optional episodes in one response; reuses internal cache checks for both
- [x] 1.6 Implement request deduplication with `Dictionary<int, List<IActorRef>>` waiter lists — second request for same inflight tvdbId adds sender to waiters; on API result persist event and notify all waiters
- [x] 1.7 Implement episode request deduplication with composite key `(tvdbId, season)` — same waiter pattern as show info
- [x] 1.8 Handle API failures — reply with empty/null results, log warning, do NOT persist events for failed lookups, clear waiter list
- [x] 1.9 Register `TvdbGatewayActor` in `FunkArrActorSystemSetup` via Akka.Hosting
- [x] 1.10 Write tests for `TvdbGatewayActor` — cache hit, cache miss, TTL expiry, request dedup, API failure, recovery from journal replay

## 2. TmdbGatewayActor

- [x] 2.1 Create `TmdbGatewayActor` as `ReceivePersistentActor` with PersistenceId `"tmdb-gateway"` — event: `MovieInfoCached`; state: `Dictionary<string, CachedMovieInfo>` keyed by ImdbId; no snapshots
- [x] 2.2 Create `TmdbGatewayActorState` with `Apply` method for `MovieInfoCached` (dictionary assignment); `IsExpired(imdbId)` using 24h TTL
- [x] 2.3 Implement `GetMovieInfo`/`MovieInfoResponse` handler — check cache, if expired/missing call `TmdbClient.FindByImdbIdAsync` via PipeTo, persist, reply
- [x] 2.4 Implement `SearchMovie`/`MovieInfoResponse` handler — calls `TmdbClient.SearchMovieAsync`, persists result keyed by resolved ImdbId if available
- [x] 2.5 Implement request deduplication with waiter lists keyed by ImdbId
- [x] 2.6 Handle API failures and no-API-key scenario — reply with empty results, no events persisted
- [x] 2.7 Register `TmdbGatewayActor` in `FunkArrActorSystemSetup` via Akka.Hosting
- [x] 2.8 Write tests for `TmdbGatewayActor` — cache hit/miss, TTL expiry, text search, dedup, API failure, no API key, recovery

## 3. RuleSetRegistryActor hash-based diffing

- [x] 3.1 Add hash tracking to `RuleSetRegistryActorState` — `Dictionary<string, RuleSetEntry>` where `RuleSetEntry(Hash, RuleSetFile)`; `Apply` methods for `RuleSetLoaded`, `RuleSetUpdated`, `RuleSetRemoved`
- [x] 3.2 Create new event records: `RuleSetLoaded(EntityKey, Hash, RuleSetFile, LoadedAtUtcTicks)`, `RuleSetUpdated(EntityKey, Hash, RuleSetFile, UpdatedAtUtcTicks)`, `RuleSetRemoved(EntityKey, RemovedAtUtcTicks)`
- [x] 3.3 Replace `LoadCommunityFromDisk` + `PushToMediaActors` with hash-diffing logic — compute SHA256 per file, compare against state hashes, persist only changed/new/removed, push only affected rules
- [x] 3.4 Remove `CommunityBatchLoaded` event, `CommunityRuleSets` list from state, and `CommunityVersion` tracking — replaced by per-file event model
- [x] 3.5 Remove snapshot logic — delete `RegistrySnapshot`, `ToSnapshot`/`FromSnapshot`, `SnapshotInterval`, `SaveSnapshotSuccess`/`SaveSnapshotFailure` handlers
- [x] 3.6 Update recovery to replay `RuleSetLoaded`/`RuleSetUpdated`/`RuleSetRemoved` events, then diff against current disk state
- [x] 3.7 Update `HandleGetCatalog` to read from the new `Dictionary<string, RuleSetEntry>` state instead of `CommunityRuleSets` list
- [x] 3.8 Update `HandleRefreshComplete` to trigger hash-diff reload instead of clear-and-reload
- [x] 3.9 Write tests — initial load persists events with hashes, refresh with no changes produces no events, refresh with one change produces one event, file removal detected, recovery replays correctly

## 4. ShowActor simplification

- [x] 4.1 Remove `TvdbClient` from constructor — resolve `TvdbGatewayActor` from `IReadOnlyActorRegistry` instead
- [x] 4.2 Replace `HandleResolveSearch` — ask `TvdbGatewayActor.ResolveShow(tvdbId, season)` via PipeTo instead of calling `TvdbClient` directly; cache response in transient RAM fields (`_showName`, `_episodes`, `_gatewayFetchedAt`)
- [x] 4.3 Remove `LookupTvShowAsync`, `FetchEpisodesAndReply`, `HandleTvdbLookupResult` methods — replaced by gateway ask
- [x] 4.4 Make `HandleApplyCommunityRules` transient — update RAM state and recompute effective rules without calling `Persist`
- [x] 4.5 Remove `ShowResolved` event, `CommunityRulesApplied` event from event schema — keep only `RulesGenerated`, `LocalOverrideApplied`, `LocalOverrideRemoved`, `MatchQualityRecorded`
- [x] 4.6 Remove snapshot logic — delete `ShowActorSnapshot`, `ToSnapshot`/`FromSnapshot`, `SnapshotInterval`, `SaveSnapshotSuccess`/`SaveSnapshotFailure` handlers, `IncrementAndSnapshot` method
- [x] 4.7 Simplify `ShowActorState` — remove `ShowName`, `ShowResolvedAtUtcTicks`, `EpisodesBySeason`, `IsShowResolved`, `IsShowExpired` (all now transient on the actor); remove `CommunityRuleSet` from persistence (still in RAM)
- [x] 4.8 Update recovery — only recover `RulesGenerated`, `LocalOverrideApplied`, `LocalOverrideRemoved`, `MatchQualityRecorded`; community rules arrive via registry push after recovery
- [x] 4.9 Update `HandleMatch` to get episodes from transient state (fetched via gateway) instead of `_state.EpisodesBySeason`
- [x] 4.10 Update existing ShowActor tests — remove TvdbClient mocking, add TvdbGatewayActor test probe, verify no persistence of ShowResolved/CommunityRulesApplied events

## 5. MovieActor simplification

- [x] 5.1 Remove `TmdbClient` from constructor — resolve `TmdbGatewayActor` from `IReadOnlyActorRegistry` instead
- [x] 5.2 Replace `HandleResolveSearch` — ask `TmdbGatewayActor.GetMovieInfo` or `SearchMovie` via PipeTo; cache response in transient RAM fields
- [x] 5.3 Remove `LookupMovieAsync`, `HandleTmdbLookupResult` methods — replaced by gateway ask
- [x] 5.4 Make `HandleApplyCommunityRules` transient — update RAM state without `Persist`
- [x] 5.5 Remove `MovieResolved` event, `CommunityRulesApplied` event from event schema — keep only `RulesGenerated`, `LocalOverrideApplied`, `LocalOverrideRemoved`, `MatchQualityRecorded`
- [x] 5.6 Remove snapshot logic — delete `MovieActorSnapshot`, `ToSnapshot`/`FromSnapshot`, `SnapshotInterval`, handlers
- [x] 5.7 Simplify `MovieActorState` — remove `Title`, `OriginalTitle`, `ReleaseYear`, `RuntimeMinutes`, `ResolvedAtUtcTicks`, `IsResolved`, `IsExpired` (all transient); remove `CommunityRuleSet` from persistence
- [x] 5.8 Update recovery — only recover `RulesGenerated`, `LocalOverrideApplied`, `LocalOverrideRemoved`, `MatchQualityRecorded`
- [x] 5.9 Update `HandleMatch` to use transient movie info from gateway response
- [x] 5.10 Update existing MovieActor tests — remove TmdbClient mocking, add TmdbGatewayActor test probe

## 6. DI and wiring

- [x] 6.1 Update `FunkArrServiceSetup` — keep `TvdbClient` and `TmdbClient` HttpClient registrations (still used by gateways)
- [x] 6.2 Update `FunkArrActorSystemSetup` — register `TvdbGatewayActor` and `TmdbGatewayActor` as singleton actors before ShowActor/MovieActor shard regions; inject `TvdbClient`/`TmdbClient` into gateway actors instead of media actors
- [x] 6.3 Verify startup order — gateway actors registered and ready before shard regions activate; registry push happens after recovery

## 7. Integration verification

- [x] 7.1 Run full test suite — verify all existing tests pass or are updated
- [x] 7.2 Verify search pipeline end-to-end — SearchRequestActor → ShowActor → TvdbGatewayActor → Mediathek → Match flow works unchanged
- [x] 7.3 Verify community ruleset push flow — RuleSetRegistryActor loads from disk → pushes to ShowActor/MovieActor → transient rules available for matching
- [x] 7.4 Verify local override flow — save/remove local override via API → persisted in ShowActor/MovieActor journal → survives restart
- [x] 7.5 Run `dotnet format` on all modified files
