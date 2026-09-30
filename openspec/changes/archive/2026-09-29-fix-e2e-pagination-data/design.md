## Context

The E2E test suite generates test data by triggering Sonarr episode searches
(which produce scoring entries and Sonarr grabs) and uploading NZB files directly
via SABnzbd API (which produce download history entries). The current approach
doesn't generate enough entries to cross pagination boundaries:

- Activity History: pageSize=25 in `Activity.vue`, only ~3 entries generated
- Scoring History: pageSize=20 in `ScoringHistory.vue`, only ~17 entries generated

The pagination UI logic is correct -- buttons appear only when
`totalCount > pageSize`. The test just doesn't produce enough data.

## Goals / Non-Goals

**Goals:**
- Reliably generate >25 download history entries for history pagination test
- Reliably generate >20 scoring entries for scoring pagination test
- Mixed download categories (show + movie) for category filter test (5.3)
- Keep total E2E runtime increase under 2 minutes

**Non-Goals:**
- Changing application page sizes
- Testing with real completed downloads only (failed entries are valid history)
- Adding a second Sonarr series (Tatort alone has 1400+ episodes, more than enough)

## Decisions

### 1. History pagination via NZB upload flood

Upload 20 NZB files with unreachable URLs (`192.0.2.1`) directly via SABnzbd
addfile API. These fail within seconds (connection timeout). Split 10x
category=show and 10x category=movie.

Combined with ~3-5 entries from real Sonarr grabs and the existing failed test
NZB, this produces 26-28 history entries total.

**Why not more real Sonarr grabs?** Each grab needs a unique episode that Sonarr
decides to download. With concurrency=2 and ~30-60s per download, 25 real
downloads would add 6-12 minutes. The NZB flood adds ~30 seconds.

**Why mixed categories?** The category filter dropdown (test 5.3) only has values
when history contains multiple categories. With show-only entries, the filter
test is trivially true.

### 2. Scoring pagination via broader episode search spread

Change step 21 from "5+ more searches" to explicitly searching 15+ episodes
spread across seasons 2020-2026. The pagination cache deduplicates per
(ruleset, season, episode) tuple, so searching different episodes in different
seasons guarantees unique scoring entries.

Combined with ~5-8 entries from Phase 3 searches, this reliably produces >20.

### 3. Placement in test execution order

**History flood**: New step 18.5, after "Wait for completions + failure" (step 18)
and before "History tests" (step 19). The flood entries must exist before the
history UI tests check pagination.

**Scoring searches**: Step 21 stays in place, just with higher count. The
pagination verification in step 22 follows immediately.

## Risks / Trade-offs

- **NZB flood entries are all "Failed"**: The history will be mostly failed
  entries with a few completed. This is fine -- pagination doesn't care about
  status, and having both statuses actually strengthens the status indicator
  tests (5.5, 5.6).
- **Sonarr queue noise**: Failed NZBs that Sonarr tracked will show as failed
  in Sonarr's queue. The E2E test doesn't verify Sonarr's queue state after
  this point, so no impact.
- **Timing sensitivity**: NZB failures depend on TCP connection timeout to
  `192.0.2.1`. On most systems this is 10-30 seconds. The skill should poll
  history count rather than sleeping a fixed duration.
