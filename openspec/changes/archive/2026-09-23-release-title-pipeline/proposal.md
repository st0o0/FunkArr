## Why

Release title construction is the single biggest source of naming bugs across the codebase. In a recent audit of 69 rulesets, 25 had title problems — double S##E## (7/10 tested series), topic name appearing twice (16 rulesets), wrong movie mediaName (3 topic groups), "Staffel" metadata leaking into titles, and suffix-stripping edge cases.

All these bugs share one root cause: `ReleaseTitleBuilder.Build` receives a **raw Mediathek title** and must simultaneously decide what the episode title IS and format it. It uses subtractive heuristics (strip topic prefix/suffix, strip S##E## patterns, strip regex matches) that break on edge cases. No single component is responsible for answering "what is the clean episode title?"

The information needed for a clean title exists in the pipeline — Scoring produces a `ConstructedTitle`, Enrichment resolves a TVDB `EpisodeName` — but both are discarded before reaching the title builder. `EnrichedItem` carries `Identity` (Season/Episode) but no display-level data.

## What Changes

Introduce a `ReleaseDisplay` record that flows through the search pipeline alongside `MediaIdentity`. Each phase contributes what it knows:

- **Ruleset resolution** sets `MediaName` (the series/film name for Sonarr recognition)
- **Scoring** sets `EpisodeTitle` from `ConstructedTitle` (regex-extracted clean title)
- **Enrichment** overrides `EpisodeTitle` with the TVDB `EpisodeName` when confidence is high
- **Fallback** cleans the raw Mediathek title for items without scoring/enrichment

`ReleaseTitleBuilder` becomes a pure formatter: it receives fully-resolved display data and assembles the scene-style string. No more topic stripping, S##E## deduplication, or regex cleanup — those concerns are eliminated by having clean inputs.

## Capabilities

### New Capabilities
- `release-display`: The `ReleaseDisplay` record and the title resolution logic that populates it from scoring/enrichment results

### Modified Capabilities
- `release-title-format`: `ReleaseTitleBuilder.Build` replaced by `ReleaseTitleBuilder.Format` accepting resolved display data instead of raw Mediathek title. `StripTopicFromTitle` removed. `MetadataSpec.ConstructedTitle` field removed (moved to display pipeline). Sanitization and quality mapping preserved.

## Impact

- `FunkArr.Search`: New `ReleaseDisplay` record, `EnrichedItem` gains `Display` field, `TvSearchWorkerState` and `MovieSearchWorkerState` populate display in their `Apply` methods, `ReleaseVariant.Expand` simplified
- `FunkArr.Core`: `ReleaseTitleBuilder` rewritten as pure formatter, `StripTopicFromTitle` and S##E## stripping removed
- `FunkArr.Messages`: `MetadataSpec.ConstructedTitle` removed (no longer needed as transit field)
- Test updates: `ReleaseTitleBuilderTests` rewritten for new `Format` API, worker state tests updated for display population
- No persistence changes, no API changes, no message contract changes beyond MetadataSpec
