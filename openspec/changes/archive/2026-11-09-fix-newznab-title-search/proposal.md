# Fix Newznab tvsearch `title` Parameter Handling

## Problem

Sonarr v5 sends `title=Tatort` instead of (or in addition to) `tvdbid=83214` in
tvsearch requests. FunkArr binds `title` into `IndexerRequest.Title` but never reads
it — `IsWithoutSearchCriteria` only checks `q`, `tvdbid`, `imdbid`, `tmdbid`. A
tvsearch with only `title` falls through to the feed/validation path, returning 2620
unfiltered entries. Sonarr can't match specific episodes in that haystack.

Even when using `tvdbid`, the returned items often lack `season` and `episode`
Newznab attributes because TVDB enrichment may fail or return different season
numbers than what Sonarr expects.

**Result**: Sonarr episode searches return 0 reports downloaded — the core use case
of FunkArr (Sonarr integration) is broken for manual searches.

## Root Cause Analysis

Three issues in the request→response chain:

```
1. title ignored          NewznabSearchService.IsWithoutSearchCriteria
                          doesn't check req.Title → feed fallback

2. Feed not filtered      Feed/validation path returns ALL entries
                          regardless of title parameter

3. Missing S/E attrs      Items from title-strategy scoring have
                          Identity.Season/Episode = null when TVDB
                          enrichment fails or returns mismatched seasons
```

## Solution

### Part A: Map `title` to `q` (essential)

In `NewznabSearchService.Search()`, before `IsWithoutSearchCriteria`: if `req.Title`
is set and `req.Q` is empty, copy `req.Title` into `req.Q`. This triggers a
MediathekViewWeb topic search instead of the feed fallback.

### Part B: Filter feed entries by title (essential)

When the search does fall through to the feed path (e.g. no RuleSet matched the
query), filter the feed entries by title/topic before returning. Currently
`QueryFeed` only filters by `MediaType`.

### Part C: Airdate-based season/episode fallback (recommended)

When enrichment fails to produce season/episode (e.g. TVDB returns season 1 but
Sonarr asks for season 2026), synthesize season/episode from the item's `AiredAt`:
- Season = `AiredAt.Year`
- Episode = sequential number within that year (from TVDB episode list), or MMDD
  fallback

This gives Sonarr something to match against, even without a TVDB season hit.

**Why MMDD is problematic**: Sonarr sends `ep=18` for episode 18, not `ep=0118`
for January 18. MMDD encoding only works for daily-type series where Sonarr sends
`season=YYYY&ep=MMDD`. For standard-type series (which is what Tatort is configured
as in most Sonarr setups), Sonarr sends literal episode numbers.

**Recommended approach**: Emit `season=AiredAt.Year` and `episode=<TVDB episode
number within that year>` when TVDB enrichment resolves the episode but the TVDB
season number doesn't match the requested season. The TVDB data IS available — we
just need to use the episode's air year as the season number instead of TVDB's
season number.

## Scope

- `FunkArr.ArrApi/Newznab/Services/NewznabSearchService.cs` — title→q mapping,
  feed filtering
- `FunkArr.Search/Workers/TvEvaluationState.cs` — season remapping after enrichment
- `FunkArr.ArrApi/Newznab/Services/NewznabAttributeBuilder.cs` — airdate fallback
  for attributes
- `FunkArr.Feed/FeedStoreManagerState.cs` — add title filter to `QueryFeed`
- Tests in ArrApi.Tests and Search.Tests

## Out of Scope

- Fixing TVDB's season layout for Tatort (data issue)
- Changing how Sonarr decides which parameter to send (Sonarr behavior)
- Movie search `title` parameter (same pattern but lower priority)
