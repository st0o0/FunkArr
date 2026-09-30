## Context

FunkArr's ruleset and match observability is split across two UI tabs (Rulesets and Matches) with separate backend endpoints. The data model supports rich match traces (filter checks, strategy detail, per-rule pipeline) but the presentation layer only surfaces shallow summaries. Key data that could drive actionable insights — episode coverage, match rate trends, failure patterns — isn't tracked or aggregated.

The backend actors (ShowActor, MovieActor, RecentMatchActor) already hold most of the raw data. The primary work is: (1) extending actor state with a few new tracked fields, (2) adding aggregation/analysis endpoints, and (3) rebuilding the frontend around a unified progressive-disclosure model.

## Goals / Non-Goals

**Goals:**
- Unified ruleset + match observability in a single UI area
- Episode-level coverage visibility (which episodes have ever been matched)
- Match rate trends over time per ruleset
- Automated failure pattern classification with actionable suggestions
- Cross-ruleset health dashboard with fleet-level metrics
- Enriched rule cards showing per-rule effectiveness and recent matches
- Item-level forensics with full pipeline trace inline

**Non-Goals:**
- Mediathek discovery (detecting untracked shows) — future work
- Download-to-match linkage (connecting match → download success) — future work
- Smart suggestions that auto-fix rules — out of scope
- Historical data backfill for existing rulesets — new tracking starts from deployment
- Changes to the matching engine logic itself — only observability additions
- Changes to the RuleSetEditor or RulesetCreationWizard — existing editing flows unchanged

## Decisions

### Decision 1: Extend ShowActor/MovieActor state rather than a new actor

**Choice:** Add episode coverage, match rate history, and per-rule recent matches directly to ShowActorState/MovieActorState.

**Why not a separate actor?** The data is tightly coupled to each show's lifecycle — it needs the TVDB episode list for coverage, the match results for trends, and the rule indices for per-rule stats. A separate actor would need cross-actor queries for every detail view load. ShowActor already processes matches and persists MatchQualityRecorded events; extending that event with richer data is natural.

**Trade-off:** ShowActor state grows, but the additions are bounded (HashSet of matched episodes, capped list of 90 snapshots, CircularQueue of 5 strings per rule). Memory impact is negligible per actor.

### Decision 2: Failure pattern analysis as stateless service, not actor

**Choice:** The failure classification endpoint runs stateless logic over unmatched items fetched from RecentMatchActor. No new actor, no persistence.

**Why:** Failure patterns are derived from existing ItemEvaluation data. The classification logic (duration-based, title-format, no-tvdb-match, etc.) is deterministic and cheap to compute on each request. Caching isn't needed because the underlying data changes infrequently and the analysis is fast.

**Alternative considered:** Persisting failure analysis in RecentMatchActor. Rejected — adds persistence complexity for data that's trivially recomputable.

### Decision 3: Fleet stats as aggregation over existing GetMatchQuality

**Choice:** The fleet stats endpoint queries all ShowActors/MovieActors via the RuleSetRegistryActor catalog and aggregates their match quality responses.

**Why:** The registry already knows all entity keys. Each actor already responds to GetMatchQuality. The aggregation is a fan-out query with bounded concurrency.

**Trade-off:** Fleet stats response time scales with the number of active rulesets. For ~60 rulesets this is fast. If it grows to hundreds, we may need a cached aggregation actor. Acceptable for now.

### Decision 4: Match rate snapshots triggered after search, not on timer

**Choice:** Record a MatchRateSnapshot after each search evaluation, but only if the last snapshot is older than 6 hours.

**Why:** Timer-based snapshots would fire even when nothing happened. Search-triggered snapshots naturally align with activity. The 6-hour throttle prevents excessive events during burst search periods. 90-entry cap (~3 weeks at 4/day) keeps state bounded.

**Alternative considered:** Daily timer via scheduler. Rejected — adds startup complexity and can miss activity windows. The throttled-on-activity approach is simpler and more informative.

### Decision 5: Merge Matches tab into Rulesets, kill standalone match routes

**Choice:** Remove the Matches tab from navigation. Match history, topic stats, and unmatched items become sub-sections of each ruleset's detail view. The fleet overview replaces the topics sub-view.

**Why:** Match data only makes sense in the context of a ruleset. Navigating between two tabs to understand one ruleset's performance is the core UX problem. Unifying them eliminates context-switching.

**Migration:** The `/#/matches/:id` deep link can remain as a redirect to the corresponding ruleset's match history with that evaluation highlighted. `/#/matches` redirects to `/#/rulesets`.

### Decision 6: Episode coverage tracking uses a simple HashSet, not a time-series

**Choice:** Track `HashSet<(int Season, int Episode)>` of ever-matched episodes plus `Dictionary<(int, int), DateTime>` for last-seen timestamps.

**Why:** The primary question is binary: "was this episode ever matched?" The last-seen timestamp adds recency context. Full time-series (when was it first seen, how many times matched) is interesting but adds significant state for marginal value.

**Trade-off:** We can't answer "when was this episode first matched?" Only "was it ever matched and when was the last time?" Acceptable for v1.

### Decision 7: Unmatched item recurrence in RecentMatchActor

**Choice:** Extend the per-topic unmatched tracking with a seen-count and first-seen timestamp per unique item (keyed by title + topic).

**Why:** Knowing an item has been unmatched 5 times across 3 days (vs. once) is critical for prioritizing rule fixes. Items that recur are worth fixing; one-offs may be transient Mediathek content.

**Impact:** Slight increase in RecentMatchActor state size. Bounded by the existing 50-items-per-topic cap.

### Decision 8: Per-rule recent matches as CircularQueue in ShowActorState

**Choice:** Store the last 5 matched episode names per rule index in ShowActorState using `Dictionary<int, CircularQueue<string>>`.

**Why:** The rule card needs to show "what did this rule actually match?" to give users confidence in rule behavior. 5 items is enough for a quick glance without excessive state.

**Impact:** Extends the existing MatchQualityRecorded event with additional data. Backward-compatible — recovery handles missing fields via nullable defaults.

## Risks / Trade-offs

**[Risk] Fleet stats fan-out may be slow with many rulesets** → Mitigation: Start with direct fan-out. If >100 rulesets cause latency issues, add a FleetStatsActor that caches aggregated stats with a 5-minute TTL.

**[Risk] Episode coverage set grows unbounded for long-running shows** → Mitigation: For shows like "Tatort" with 1200+ episodes, the HashSet is still small (~50KB). No cap needed.

**[Risk] Removing Matches tab may break bookmarks/external links** → Mitigation: Keep `/#/matches` and `/#/matches/:id` as redirects to the new locations. Log a console warning for deprecation.

**[Risk] MatchRateSnapshot events accumulate over time** → Mitigation: 90-entry cap in state. Old snapshots are evicted on apply. The event journal grows but events are small. No snapshot deletion needed — recovery replays all and caps in state.

**[Risk] Failure pattern classification may miscategorize items** → Mitigation: Classification is heuristic-based and visible to the user. Wrong classifications are informational, not actionable without user confirmation. Start with conservative patterns and iterate.

## Open Questions

- **Sparkline rendering library:** Use inline SVG (no dependency) or a lightweight chart component? Leaning toward inline SVG for the sparklines and CSS-only for progress bars.
- **Fleet stats caching:** Should we add a short-lived cache (1-5 min) on the fleet stats endpoint from day one, or wait until it proves necessary? Leaning toward no cache initially.
