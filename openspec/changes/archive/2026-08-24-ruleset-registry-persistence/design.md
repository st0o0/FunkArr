## Context

The RuleSetRegistryActor currently loads community rulesets from disk and tracks local overrides in a volatile in-memory dictionary. On restart, the catalog loses all local entries. The save flow uses fire-and-forget Tell, causing race conditions where the UI reads stale data. There is no API key enforcement and no way to export rulesets as JSON.

## Goals / Non-Goals

**Goals:**
- Make the RuleSetRegistryActor event-sourced so the catalog survives restarts
- Switch save/delete flows from Tell to Ask for consistency guarantees
- Add ruleset JSON export for community contribution workflow
- Implement API key authentication middleware
- Fix frontend bugs (RulesetDetail unwrapping, Recent tab missing endpoint)

**Non-Goals:**
- Changing ShowActor/MovieActor persistence model (they remain the source-of-truth for ruleset content and match quality)
- File-based local ruleset storage (locals stay in the actor journal)
- Merging community and local rulesets (local always wins as override)
- Rate limiting or advanced auth (simple API key check is sufficient)

## Decisions

### 1. RuleSetRegistryActor becomes ReceivePersistentActor

The registry gets `PersistenceId = "ruleset-registry"` and persists catalog-level events. It does NOT store full RuleSetFile content — only lightweight summaries (entityKey, mediaType, name, source) for catalog queries.

**Why not store full rulesets in the registry?** The ShowActor/MovieActor already own the full RuleSetFile in their journals. Duplicating it in the registry would create two sources of truth for content. The registry only needs to know *what exists* and *where it came from*.

**Events:**
- `CommunityBatchLoaded(string Version, CatalogEntry[] Entries)` — persisted when community refresh completes with a new version. Contains only summaries, not full rulesets.
- `LocalRegistered(string EntityKey, string MediaType, string Name)` — persisted when a user saves a local override.
- `LocalRemoved(string EntityKey)` — persisted when a user deletes a local override.

**Snapshots:** Every 100 events. Snapshot contains the full `_catalog` dictionary.

**Recovery:** On startup, the actor recovers from journal/snapshot, then loads community from disk (overwriting the recovered community entries with fresh disk state — community files are the source-of-truth for community content, the journal just tracks the version).

**Alternatives considered:**
- Making the registry a `ReceiveActor` that rebuilds local state by querying all ShowActors on startup — too slow, requires knowing all entity keys upfront, and creates a thundering herd on restart.
- Storing the full catalog on disk as JSON — adds a second persistence mechanism alongside Akka.Persistence; using the same journal keeps it consistent.

### 2. Save/Delete becomes Ask-based via the registry

Current flow: Controller → Tell ShowActor + Tell Registry → 200 OK (no confirmation).

New flow: Controller → Ask Registry.SaveLocal(key, ruleSet) → Registry persists LocalRegistered + forwards ApplyLocalOverride to ShowActor → Reply to controller → 200 OK.

The registry is the single entry point for writes. This ensures the catalog is updated before the controller responds.

**Why route through the registry instead of Ask-ing the ShowActor directly?** The registry needs to know about the local override for catalog queries. If we Ask the ShowActor and Tell the registry, we still have the consistency gap. Making the registry the coordinator eliminates the gap.

**Timeout:** 5 seconds. If the registry doesn't respond, the controller returns 503.

### 3. Export endpoint returns RuleSetFile JSON

`GET /api/v1/rulesets/{tvdbId}/export` asks the ShowActor for its effective RuleSetFile and returns it with `Content-Disposition: attachment; filename="{topic}.json"` and `Content-Type: application/json`.

No new actor logic needed — ShowActor already has `GetRuleSet` that returns the effective rules. The controller just adds the download headers.

### 4. API key middleware

A simple middleware registered before `MapControllers()` that:
1. Checks if the request path starts with `/api/` or `/index/` (Newznab) or `/download/` (SABnzbd)
2. If yes, reads `apikey` from query string
3. Compares against `FunkArrOptions.ApiKey`
4. Returns 401 if missing/invalid

Exemptions: `/api/v1/setup/status` (needed before auth is configured), health checks, metrics, static files, SPA fallback.

The Newznab caps endpoint (`?t=caps`) is also exempted per Newznab convention — clients use it to discover capabilities before authenticating.

### 5. Recent matches endpoint

The `GET /api/v1/matches/recent` endpoint currently returns a stub `Array.Empty<MatchRecord>()`. The MatchRecord model exists but nothing collects or stores match data.

Implementation: The ShowActor/MovieActor already process search results and produce match traces. We add a ring buffer (last N match records) to the actor state, persisted via a `MatchRecordAdded` event. The recent endpoint queries the registry for all known entity keys, then fans out Ask calls to ShowActors to collect their recent records, merges by timestamp.

**Alternative considered:** Central match ledger actor — adds complexity and another persistence stream. Keeping match history in the ShowActor is simpler and consistent with the "ShowActor owns its domain" pattern.

### 6. RulesetDetail.vue fix

The frontend assigns `response` directly as `RuleSetFile` but the API returns `RuleSetResponse { ruleSet, source, matchQuality }`. Fix: unwrap `response.ruleSet` before assigning. The existing code at line 61 already does partial unwrapping — it just needs to be consistently applied in all code paths.

## Risks / Trade-offs

- **Registry journal grows over time** — community batch events contain summaries for ~150+ rulesets each refresh. Mitigated by snapshots every 100 events and the fact that community refreshes are infrequent (hourly).
- **Ask-based save adds latency** — one extra hop (controller → registry → ShowActor) vs. direct Tell. Acceptable for a user-initiated save operation (sub-second).
- **Fan-out for recent matches** — querying all ShowActors for recent records is O(N) in catalog size. For the current scale (~150 rulesets) this is fine. If it becomes a bottleneck, a dedicated ledger actor can be added later.
- **No auth on caps** — per Newznab convention, caps is public. Prowlarr needs it to discover the indexer before configuring the API key.

## Migration Plan

No data migration needed. The registry starts with an empty journal and rebuilds:
1. Community entries come from disk loading (always happens at startup)
2. Existing local overrides in ShowActor journals are NOT automatically registered in the registry catalog — they survive in the ShowActor but won't appear in the catalog until re-saved

This is acceptable because version is 0.x and breaking changes are fine. Users who have local overrides can re-save them through the UI.

## Open Questions

None — all decisions made during explore phase.
