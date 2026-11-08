## Why

Movie enrichment via TMDB fails for re-broadcasts because `MovieEnricher.ValidateYear` compares the Mediathek broadcast date against the TMDB production year. A film from 2021 broadcast in 2026 has a 5-year gap, exceeding the default tolerance of 1 — so enrichment rejects the candidate, the production year never reaches the Newznab title, and Radarr rejects all results. This breaks the entire Radarr integration for movies.

## What Changes

- Remove year validation from `MovieEnricher.ResolveCandidate` — the comparison between broadcast date and production year is conceptually wrong for movies. Title similarity is a sufficient guard.
- Remove the `YearMatch` fallback path that also relies on this flawed comparison.
- Fix title stutter in `SceneRelease.ForMovie` where `MediaName` and `EpisodeTitle` are identical (e.g., `Schachnovelle.2026.Schachnovelle.GERMAN...`).

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `movie-resolution`: Remove year-based candidate rejection from movie enrichment matching. Title similarity threshold remains the sole guard.
- `scene-release`: Deduplicate movie titles when the identified episode title matches the media name.

## Impact

- `src/FunkArr.Enrichment/Tmdb/MovieEnricher.cs` — remove `ValidateYear` and `YearMatch` path
- `src/FunkArr.Search/Models/SceneRelease.cs` — deduplicate title in `ForMovie`
- `src/FunkArr.Enrichment.Tests/` — update MovieEnricher tests
- `src/FunkArr.Search.Tests/Models/SceneReleaseTests.cs` — add title dedup test
- No API changes, no persistence changes, no message changes
