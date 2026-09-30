## Context

Match data is produced inside ShowActor/MovieActor during search-triggered matching but never collected. The MatchIntelligenceController has two stub endpoints returning empty arrays. The MatchesView.vue frontend expects full MatchRecord objects with matched/filtered/unmatched trace arrays.

## Goals / Non-Goals

**Goals:**
- Collect match results from ShowActor/MovieActor into a central actor
- Serve recent matches and unmatched items via the existing API endpoints
- Keep the ring buffer bounded (100 entries max)

**Non-Goals:**
- Historical analytics or long-term match storage
- Per-topic match history (TopicStats already handled by ShowActor)
- Changing the search pipeline or SearchRequestActor

## Decisions

### 1. RecentMatchActor as event-sourced singleton

`ReceivePersistentActor` with `PersistenceId = "recent-match-actor"`, registered via `WithResolvableActors`.

**State:**
- `_recentRecords`: `List<MatchRecord>` capped at 100 (ring buffer — oldest evicted on overflow)
- `_unmatchedByTopic`: `Dictionary<string, List<UnmatchedTrace>>` — last N unmatched per topic, capped at 50 per topic

**Events:**
- `MatchRecordAdded(MatchRecord Record)` — single event per match operation

**Snapshots:** Every 50 events. Snapshot contains both collections.

**Why event-sourced and not just in-memory?** Survives restarts. Without persistence, a restart clears all recent data until new searches execute.

### 2. ShowActor/MovieActor produce full traces and Tell

The ShowActor.HandleMatch currently calls `EvaluateRulesWithoutTvdb` which returns only matched items (no traces). To provide filtered/unmatched data:

- ShowActor switches to `EvaluateRulesWithTraces`, passing the flattened `_episodesBySeason` episodes. This produces `(matches, traces)` where traces include MatchedTrace, FilteredTrace, and UnmatchedTrace.
- After persisting MatchQualityRecorded and replying to Sender, the actor Tells RecentMatchActor with the full MatchRecord.
- MovieActor does the same with `EvaluateMovieRulesWithTraces` (new method, or reuse existing with trace output).

**Why not collect in SearchRequestActor?** SearchRequestActor only receives `MatchedResults` (matched items only). The ShowActor/MovieActor has the full evaluation context: rules, episodes, show name, and produces all three trace types.

### 3. MatchRecord construction in ShowActor/MovieActor

The MatchRecord is assembled right after evaluation:
```
MatchRecord {
  Id = Guid.NewGuid().ToString("N"),
  Timestamp = DateTimeOffset.UtcNow,
  SearchTopic = _effectiveTopic,
  TvdbId = int.Parse(_tvdbId),
  Season = cmd.Season,
  Episode = cmd.Episode,
  Source = source,
  TotalResults = cmd.Items.Length,
  Matched = traces.OfType<MatchedTrace>(),
  Filtered = traces.OfType<FilteredTrace>(),
  Unmatched = traces.OfType<UnmatchedTrace>(),
}
```

### 4. Controller delegates to RecentMatchActor

`GetRecent(limit)` → Ask RecentMatchActor → returns last N records sorted by timestamp desc.
`GetUnmatched(topic?)` → Ask RecentMatchActor → returns unmatched groups, optionally filtered.

Single Ask, no fan-out. Simple and fast.

### 5. EvaluateMovieRulesWithTraces

The existing `EvaluateMovieRules` returns only matched items. We need a trace-producing variant. Pattern matches `EvaluateRulesWithTraces` but uses movie matching logic.

## Risks / Trade-offs

- **Slightly more work in HandleMatch** — full trace evaluation is heavier than match-only, but the item counts per search are small (typically <200 items) and this already happens in TestRules.
- **100-record cap** — old records are evicted. This is intentional — recent matches is a debugging/observability tool, not a log.
- **Unmatched per-topic cap of 50** — prevents memory growth from noisy topics. Newest items replace oldest.

## Open Questions

None.
