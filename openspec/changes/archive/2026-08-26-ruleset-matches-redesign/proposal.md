## Why

The Rulesets and Matches areas are disconnected — two separate tabs with weak cross-linking, shallow failure diagnostics ("filter failed" / "no match" without the why), no health overview, no trends, and no episode coverage visibility. Users must bounce between views to understand ruleset performance, and the MatchDetailView fetches all recent records client-side (record gone = 404). Most of the data needed for rich observability already exists in ShowActor/RecentMatchActor but isn't surfaced.

## What Changes

- **Merge Matches into Rulesets**: Kill the separate Matches tab. Match history, topic stats, and unmatched items become sub-sections of each ruleset's detail view.
- **Fleet overview dashboard**: Top-level rulesets view gains stat cards (total rulesets, avg match rate, attention count, unmatched count) and enriched list cards with inline progress bars, status indicators, and last activity timestamps.
- **Episode coverage tracking**: ShowActor/MovieActor track which TVDB/TMDB episodes have ever been matched, exposed via a coverage grid on the detail view.
- **Match rate trend snapshots**: Periodic snapshots of match quality for sparkline/trend visualization on ruleset detail.
- **Failure pattern analysis**: Stateless classification of unmatched items by failure signature (duration too short, title format mismatch, no TVDB match, etc.) with actionable suggestions.
- **Fleet stats aggregation**: Cross-ruleset aggregation endpoint for the health dashboard (breakdowns by source, strategy, channel; worst performers).
- **Enriched rule cards**: Per-rule stats show recent matched episode names, dead rule warnings, and redundancy detection.
- **Item-level forensics**: Full pipeline trace inline in the detail view — every filter check, every strategy attempt, recurrence tracking ("seen 5x in 3 days").
- **Direct match fetch**: RecentMatchActor supports fetch by ID instead of client-side filtering.
- **Unmatched item recurrence**: Track how many times an unmatched item has appeared and when it was first seen.

## Capabilities

### New Capabilities

- `episode-coverage`: ShowActor/MovieActor persist which episodes have been matched, with a coverage grid API endpoint and UI visualization
- `match-rate-trends`: Periodic match quality snapshots on ShowActor/MovieActor with trend API endpoint for sparkline/chart rendering
- `failure-pattern-analysis`: Stateless classification of unmatched items into failure patterns (duration_filter, title_format, no_tvdb_match, channel_filter, content_type) with per-pattern suggestions and a dedicated API endpoint
- `fleet-stats`: Aggregation endpoint computing cross-ruleset health metrics — totals, averages, breakdowns by source/strategy/channel, and worst performers list
- `ruleset-health-views`: Redesigned frontend replacing the separate Matches tab — fleet overview dashboard, enriched ruleset detail with coverage/trends/failures/history sub-sections, and inline item-level forensics

### Modified Capabilities

- `show-actor`: New state fields for episode coverage tracking (MatchedEpisodes set), match rate history (snapshot list), and per-rule recent matches (last 5 episode names per rule)
- `movie-actor`: Parallel state changes for movie coverage and trend tracking
- `recent-match-actor`: Add unmatched item recurrence tracking (seen count + first seen) and direct fetch by evaluation ID
- `ruleset-api`: New endpoints — GET coverage, GET trend, GET failures, GET fleet stats
- `match-intelligence-api`: Add GET by ID endpoint for direct evaluation fetch
- `web-ui-shell`: Remove Matches tab from navigation, restructure routing so match-related views live under rulesets
- `persistence-dtos`: New event types for EpisodeMatched and MatchRateSnapshotRecorded

## Impact

- **Actors**: ShowActor, MovieActor, RecentMatchActor gain new state fields and events. Existing persistence is extend-only (nullable new fields, default values). No migration needed (0.x version).
- **API**: New endpoints under existing `/api/v1/rulesets/` and `/api/v1/matches/` prefixes. Existing endpoints unchanged. All authenticated via existing ApiKeyMiddleware.
- **Frontend**: Major restructuring of Vue SPA — Matches tab removed, Rulesets view and detail view rebuilt, new components for coverage grid, trend sparkline, failure patterns, and forensics panels. Router changes (match routes removed, ruleset routes extended).
- **Persistence DTOs**: Two new event record types. Extend-only, no breaking changes to existing events.
- **No external dependency changes.**
