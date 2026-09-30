## Why

ShowActor (473 lines, 11 messages) and MovieActor (453 lines) are the same class written twice — six byte-identical event records, duplicated `BuildTopicStats`, duplicated match flow, duplicated metadata caching that duplicates what the gateway actors already provide. The design document (§5, §8) mandates extracting a shared abstract base with thin subclasses to enable the "new media kind = ~50 lines" extension seam, and separating analytics from the entity so each responsibility has its own lifecycle.

## What Changes

- **New abstract `MediaRuleSetActor`** (~120–150 lines) holding three layer slots, persistence, the match pipeline, and passivation. Four hooks: `ResolveMetadata`, `BuildSearchHint`, `SelectStrategies`, `ComputeCoverage`.
- **`SeriesRuleSetActor`** (~50 lines) replaces `ShowActor`. Typed id `int`, uses `TvdbGatewayActor`.
- **`MovieRuleSetActor`** (~50 lines) replaces `MovieActor`. Typed id `string`, uses `TmdbGatewayActor`.
- **`MatchStatsActor`** replaces `RecentMatchActor`. Single event `MatchRunRecorded`. Rule-hit counts, episode coverage, and match-rate history become projections.
- **Event recut**: entity keeps only `RulesGenerated` + `LocalOverrideChanged`. Analytics events (`MatchQualityRecorded`, `EpisodeMatched`, `MatchRateSnapshotRecorded`) move to `MatchStatsActor`. **BREAKING** — persistence schema changes.
- **Metadata cache removed** from entity state. `TvdbGatewayActor`/`TmdbGatewayActor` are already the cache.
- **Persistence DTOs split**: one journal file per actor. `ShowActorJournal.cs` stops serving movies.
- **State files consolidated**: `ShowActorState` + `MovieActorState` → shared `MediaRuleSetActorState`.
- **Sharding registration updated**: `ShowActor` → `SeriesRuleSetActor`, `MovieActor` → `MovieRuleSetActor`.

## Capabilities

### New Capabilities
- `media-ruleset-actor`: Abstract base actor for media entity rule-set management — layer slots, persistence, match pipeline, four-hook contract.
- `match-stats-actor`: Centralized match analytics actor replacing per-entity stats and RecentMatchActor.

### Modified Capabilities
- `show-actor`: Replaced by `SeriesRuleSetActor` subclass of `MediaRuleSetActor`. Reduced to ~50 lines with four hook implementations.
- `movie-actor`: Replaced by `MovieRuleSetActor` subclass of `MediaRuleSetActor`. Reduced to ~50 lines with four hook implementations.
- `recent-match-actor`: Absorbed into `MatchStatsActor` with new event model.
- `persistence-dtos`: Split into per-actor journal files. New DTOs for `MatchStatsActor`. Movie gets own journal.
- `ruleset-registry`: Updated references from `ShowActor`/`MovieActor` to `SeriesRuleSetActor`/`MovieRuleSetActor`.
- `episode-coverage`: Coverage tracking moves from entity state to `MatchStatsActor` projection.
- `match-rate-trends`: Match-rate history moves from entity state to `MatchStatsActor` projection.

## Impact

- **Code**: `RuleSet/` folder — ShowActor.cs, MovieActor.cs, RecentMatchActor.cs and their state files deleted; replaced by MediaRuleSetActor.cs, SeriesRuleSetActor.cs, MovieRuleSetActor.cs, MatchStatsActor.cs + state files.
- **Persistence**: Journal schema breaking change (acceptable at 0.x). New persistence IDs (`ruleset-series-{id}`, `ruleset-movie-{id}`, `match-stats`).
- **Sharding**: Region registrations updated in `FunkArrActorSystemSetup`.
- **Tests**: Actor tests renamed and updated. Journal round-trip tests updated for new DTOs.
- **API**: No external API changes — controllers route through the same shard interfaces.
