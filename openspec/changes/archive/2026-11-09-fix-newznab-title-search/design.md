# Design: Newznab Title Search Fix

## Part A: Map `title` to `q`

### Location

`NewznabSearchService.Search()`, before the `IsWithoutSearchCriteria` check.

### Change

```
// Pseudo-code — before existing logic:
if (req.Q is null or "" && req.Title is not null and not "")
    req = req with { Q = req.Title };
```

`IndexerRequest` is a record so this needs a `with` expression or a mutable approach.
After this, the existing `BuildTvSearch` path picks up `req.Q` and creates a
`SearchCommand` with the topic query, triggering MediathekViewWeb search via
`TvSearchWorker`.

### Why not check `title` in `IsWithoutSearchCriteria`?

If we add `title` to the criteria check but don't use it anywhere, the search would
still fall through to `BuildTvSearch` where `req.Q` is null. Mapping title→q first
is cleaner — one change, the rest of the pipeline works.

## Part B: Feed title filter

### Location

`NewznabSearchService.FeedOrValidation()` and/or `FeedStoreManagerState.QueryFeed()`.

### Current behavior

`QueryFeed(MediaType, offset, limit)` returns ALL entries of the given media type.
No title/topic filter.

### Change

Add an optional `topic` parameter to `QueryFeed`. When provided, filter entries where
`entry.Topic` contains the query (case-insensitive). This covers the case where:
1. title→q mapping triggers a MediathekViewWeb search
2. No RuleSet matches → scoring returns 0 items
3. The search falls through to FeedOrValidation
4. Now the feed entries are at least filtered by topic

Alternatively, filter in `NewznabSearchService` after getting feed results — simpler,
no message changes needed:

```
var feedItems = response.Items;
if (req.Q is not null)
    feedItems = feedItems.Where(i => i.Topic.Contains(req.Q, OrdinalIgnoreCase));
```

**Recommendation**: Filter in NewznabSearchService (simpler, no actor message changes).

## Part C: Season/Episode Airdate Fallback

### The problem in detail

```
Sonarr sends: season=2026, ep=18

TVDB Tatort layout options (we can't control this):
  Option 1: Season=2026, Episodes numbered 1-40   → works naturally
  Option 2: Season=1, Episodes numbered 1-1300+   → season mismatch

Current enrichment flow:
  1. TvdbEnrichmentActor pre-filters episodes where SeasonNumber == 2026
  2. If TVDB uses Season=1, the pre-filter returns nothing
  3. Fallback: try ALL episodes with 0.9 penalty
  4. If title match found: Season="1", Episode="1247"
  5. FilterBySeasonEpisode: "1" != 2026 → dropped

Even successful enrichment fails the final filter!
```

### Proposed approach: Remap season to air-year after enrichment

In `TvEvaluationState.Apply(EnrichEpisodesCompleted)`: when enrichment resolves an
episode BUT the resolved season doesn't match the requested season, AND the resolved
episode has an air date in the requested year, remap:

```
// Pseudo-code in Apply(EnrichEpisodesCompleted):
foreach enriched episode:
    if (ep.Season != RequestedSeason
        && ep.AiredAt?.Year == RequestedSeason)
    {
        // TVDB season layout doesn't match Sonarr's year-as-season model
        // Remap to year-based season so FilterBySeasonEpisode can match
        ep = ep with { Season = ep.AiredAt.Year.ToString() };
    }
```

**Problem**: `EnrichedEpisode` doesn't currently carry `AiredAt`. We'd need to
either:
1. Pass `AiredAt` through the enrichment response (adds a field to the message), or
2. Look up the source item's `AiredAt` in the state (already available in `Sources`)

Option 2 is simpler — the state already has `Sources[]` indexed by the same index.

### Alternative: Emit season/episode from AiredAt when enrichment fails entirely

In `NewznabAttributeBuilder.BuildAttributes`, if `Metadata.Season` is null but
`item.AiredAt` is available:
- Emit `season = AiredAt.Year`
- Don't emit episode (can't reliably determine episode number from date alone)

This at least lets Sonarr's season-only queries work (`season=2026` without `ep`).

### Recommendation

Do both:
1. **Remap season after enrichment** (catches the TVDB-season-mismatch case)
2. **Airdate season fallback in attributes** (catches the no-enrichment case)

This gives the best coverage without breaking existing behavior.

## Data flow after all changes

```
Sonarr: ?t=tvsearch&title=Tatort&season=2026&ep=18

  Part A: title → q
      ↓
  BuildTvSearch(q="Tatort", season=2026, ep=18)
      ↓
  MediathekViewWeb topic="Tatort" → 50+ candidates
      ↓
  Scoring → matched items with title-based identity
      ↓
  TVDB Enrichment → resolves episodes
      ↓
  Part C: if TVDB season != 2026 but AiredAt.Year == 2026
          → remap season to "2026"
      ↓
  BuildAttributes: season=2026, episode=18
      ↓
  FilterBySeasonEpisode: match! ✓
      ↓
  Sonarr receives items with proper S/E attributes → grabs
```

## Files Changed

| File | Part | Change |
|---|---|---|
| `FunkArr.ArrApi/Newznab/Services/NewznabSearchService.cs` | A | title→q mapping before IsWithoutSearchCriteria |
| `FunkArr.ArrApi/Newznab/Services/NewznabSearchService.cs` | B | Filter feed results by topic when q is set |
| `FunkArr.Search/Workers/TvEvaluationState.cs` | C | Remap season to AiredAt.Year after enrichment when TVDB season mismatches |
| `FunkArr.ArrApi/Newznab/Services/NewznabAttributeBuilder.cs` | C | Airdate-year fallback for season attr when Metadata.Season is null |
| `FunkArr.ArrApi.Tests/` | A,B | Tests for title→q mapping and feed filtering |
| `FunkArr.Search.Tests/` | C | Tests for season remapping |
