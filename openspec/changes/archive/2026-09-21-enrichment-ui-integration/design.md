## Context

Enrichment resolves matched Mediathek items against external metadata (TVDB episodes, TMDB movies) using configurable methods (title similarity, airdate matching, runtime filtering). The config lives in ruleset JSON files and flows through `RuleSetMerger.BuildEnrichmentConfig()` into `EnrichmentManager` during real search pipeline execution. However, the entire API + UI layer skips enrichment: the detail API doesn't return it, create/update requests don't accept it, the test endpoint doesn't run it, and the debugger doesn't display it.

The existing test endpoint (`POST /api/rulesets/test`) is stateless — it takes ad-hoc rules + candidates and returns `ItemTrace[]` from the `ScoringManager`. Enrichment will require the endpoint to also call `EnrichmentManager` (which hits TVDB/TMDB APIs).

## Goals / Non-Goals

**Goals:**
- Enrichment config is visible and editable in the RuleSet Builder UI
- Enrichment config is persisted through the API create/update path
- The test endpoint optionally runs enrichment and returns enrichment trace data
- The debugger's full test mode shows enrichment results per matched item
- Users can tune enrichment thresholds and re-test to see impact

**Non-Goals:**
- No changes to the real search pipeline (already works)
- No client-side enrichment simulation (needs real API calls)
- No enrichment trace persistence in scoring history (separate change)
- No enrichment for items that didn't match scoring rules (enrichment only applies to scored matches)

## Decisions

### 1. Extend existing messages rather than creating new ones

Add optional `EnrichmentConfig?` and identity fields to `TestScoreItems` rather than a separate message type. The `ScoringManager` already handles `TestScoreItems` — when enrichment config is present, it runs enrichment after scoring; when absent, behavior is unchanged.

**Alternative**: New `TestScoreWithEnrichment` message → rejected because it duplicates the scoring path and the ScoringManager would need two handlers doing nearly the same thing.

### 2. Add `EnrichmentTrace?` to `ItemTrace` as a rich optional field

Extend the existing `ItemTrace` record with an optional `EnrichmentTrace` field. This keeps a single trace model across scoring history and test results. The field is null when enrichment wasn't requested or the item didn't match.

The trace record carries debug-relevant data beyond just the result:
- `Method` (MatchMethod) — TitleMatch, AirdateMatch, RegexExtracted, etc.
- `Confidence` (float) — similarity score for title, proximity for airdate
- `Enriched` (bool) — whether enrichment resolved the item
- `ResolvedSeason`, `ResolvedEpisode`, `ResolvedTitle` (string?) — resolved identifiers
- `ResolvedYear` (int?) — for movie enrichment
- `DaysDiff` (int?) — for airdate method, the actual difference in days (critical for tolerance tuning)
- `Detail` (string?) — failure reason when enrichment did not resolve ("similarity 0.65 < threshold 0.7", "no TVDB episodes found for season 3")

The RegexExtracted method (when scoring already found S/E via regex) produces a distinct trace: it's a TVDB lookup for the episode name, not a similarity match. The UI should display this differently — it's a confirmation, not a match attempt.

**Alternative**: Separate response type for enriched tests → rejected because the UI already knows how to render `ItemTrace[]` and extending it is simpler.

### 3. Enrichment runs in the test endpoint handler, not in ScoringManager

The test endpoint handler in `RuleSetApiEndpoints.cs` will orchestrate: first ask ScoringManager for scoring traces, then (if enrichment config provided) ask EnrichmentManager to enrich matched items, then merge enrichment results into ItemTraces before returning.

**Candidate mapping**: The endpoint must build `EpisodeCandidate[]` / `MovieCandidate[]` from scored `ItemTrace[]` for the enrichment request:
- `ConstructedTitle` → from `ItemTrace.Identification.Title`
- `AiredAt` → convert `TestCandidate.Timestamp` (unix seconds) to `DateTimeOffset`
- `ExistingSeason/Episode` → from `ItemTrace.Identification.Season/Episode` (when scoring already extracted these via regex, enrichment does a TVDB lookup instead of title/airdate matching)
- `Duration` → from original `TestCandidate.Duration`
- `Index` → position in the matched items array, used to correlate enrichment results back to ItemTraces

**Why**: Keeps ScoringManager focused on scoring. The real pipeline's enrichment orchestration lives in SearchWorker state machines — the test endpoint is a simpler linear flow that doesn't need that complexity. The API layer already has access to the actor registry.

**Alternative**: Push enrichment into ScoringManager → rejected because it couples scoring and enrichment in a way the real pipeline explicitly avoids (SearchWorker orchestrates both separately).

### 4. Enrichment config in API models mirrors the JSON schema structure

The API models for enrichment config will use the same field names and structure as the ruleset JSON schema (`enabled`, `methods`, `title.threshold`, `airdate.tolerance`, `runtime.tolerance`, `runtime.mode`, `year.tolerance`). This avoids any translation layer — what the UI sends is what gets serialized to disk.

### 5. Builder form section between Default Confidence and Rules

The enrichment section sits after identity/confidence and before rules, matching the logical flow: identity → confidence → enrichment → rules. It's collapsible and expanded by default (new feature, users need to discover it). Default values match `RuleSetMerger.BuildEnrichmentConfig()` defaults.

**Conditional visibility**: Per-method threshold/tolerance controls only render when that method's checkbox is checked — reduces visual noise when only one or two methods are active.

### 7. RuleSetDetail.vue shows enrichment config read-only

The detail view (not just builder) shows the enrichment config at a glance: enabled/disabled badge, active methods as tags, and key thresholds. Users should see this without entering edit mode.

### 8. Two-phase loading indicator in debugger

Since enrichment adds latency (external API calls), the full test button shows phased progress: "Scoring..." → "Enriching..." rather than a single spinner. This makes the wait transparent and helps users understand what's happening.

### 6. RuleSetDetailResult carries enrichment config

Add enrichment config to `RuleSetDetailResult` so the builder can load it in edit mode. The `RuleSetManagerState` already has access to enrichment config through `ExtractIdentity()` — extend the detail building logic to include it.

## Risks / Trade-offs

**[TVDB/TMDB API calls during test]** The test endpoint will make real external API calls when enrichment is enabled. → Mitigation: Enrichment actors already use in-memory caching (`TvdbClient.CacheEntryCount`, `TmdbClient.CacheEntryCount`), so repeated tests for the same series hit cache. The test endpoint timeout is already 15s which should be sufficient.

**[Test endpoint complexity]** Adding enrichment orchestration to the endpoint handler increases its size. → Mitigation: Extract into a helper method. The orchestration is linear (score → enrich → merge), not complex state machine logic.

**[Missing external IDs]** If a ruleset has enrichment enabled but no tvdbId/tmdbId, enrichment silently does nothing. → Mitigation: The UI should show a hint that enrichment requires external IDs to be set.
