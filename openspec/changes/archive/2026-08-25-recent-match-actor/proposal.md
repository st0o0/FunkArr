## Why

The match intelligence endpoints `GET /api/v1/matches/recent` and `GET /api/v1/matches/unmatched` return empty stubs. The MatchesView.vue Recent and Unmatched tabs show "No recent matches" / "No unmatched items" even after searches execute. Match data is produced in ShowActor/MovieActor during search but is never collected for observability.

## What Changes

- **New `RecentMatchActor`** — event-sourced singleton that collects match records from ShowActor/MovieActor. Maintains a ring buffer of the last 100 `MatchRecord` entries and aggregates unmatched items grouped by topic.
- **ShowActor.HandleMatch switches to trace-producing evaluation** — calls `EvaluateRulesWithTraces` instead of `EvaluateRulesWithoutTvdb` so full matched/filtered/unmatched traces are available, then Tells `RecentMatchActor.RecordMatch`.
- **MovieActor.HandleMatch same change** — Tells `RecentMatchActor.RecordMatch` after evaluation.
- **MatchIntelligenceController.GetRecent** — replaces stub with Ask to `RecentMatchActor`.
- **MatchIntelligenceController.GetUnmatched** — replaces stub with Ask to `RecentMatchActor`.

## Capabilities

### New Capabilities

- `recent-match-actor`: Singleton event-sourced actor collecting match records and unmatched items from ShowActor/MovieActor.

### Modified Capabilities

- `match-intelligence-api`: GetRecent and GetUnmatched delegate to RecentMatchActor instead of returning empty stubs.
- `show-actor`: HandleMatch produces full traces and Tells RecentMatchActor.
- `movie-actor`: HandleMatch produces full traces and Tells RecentMatchActor.

## Impact

- **New file:** `RuleSet/RecentMatchActor.cs` (actor + events + messages)
- **Modified:** `ShowActor.HandleMatch` — switch evaluate call, add Tell
- **Modified:** `MovieActor.HandleMatch` — same
- **Modified:** `MatchIntelligenceController` — GetRecent/GetUnmatched delegate to RecentMatchActor
- **Modified:** `FunkArrActorSystemSetup` — register RecentMatchActor as resolvable singleton
- **Modified:** `ShowActor` constructor — inject `IReadOnlyActorRegistry` for RecentMatchActor lookup
- **Modified:** `MovieActor` constructor — same
