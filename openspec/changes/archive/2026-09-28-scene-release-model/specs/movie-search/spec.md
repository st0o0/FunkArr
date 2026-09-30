## MODIFIED Requirements

### Requirement: MovieSearchWorkerState produces search results via SceneRelease

The MovieSearchWorkerState `ToSearchCompleted` method SHALL convert `EnrichedItem[]` to `SearchResultItem[]` by mapping each item through `SceneRelease.ForMovie(item, MediaName)` and calling `Expand()`. Results SHALL be ordered by Score descending. The state SHALL NOT contain an `EnsureDisplay` method. The state SHALL NOT contain an `UnscoredItems` method that builds displays. All items SHALL have `Display` set after `Apply(ScoreCompleted)`.

#### Scenario: ToSearchCompleted uses SceneRelease

- **WHEN** ToSearchCompleted is called after scoring and enrichment
- **THEN** each EnrichedItem SHALL be mapped via `SceneRelease.ForMovie(item, MediaName).Expand()` and the results sorted by Score descending

#### Scenario: Movie year from enrichment in release title

- **WHEN** enrichment resolves a movie with Year=1995 and the broadcast AiredAt is 2026
- **THEN** the release title SHALL contain "1995" (the TMDB year), not "2026"

#### Scenario: Display set for all items during scoring

- **WHEN** `Apply(ScoreCompleted)` processes items
- **THEN** ALL items (matched and unmatched) SHALL have a non-null Display using `ReleaseDisplay.From(source.Title, mediaName)` as the base, with ConstructedTitle overriding EpisodeTitle for matched items

#### Scenario: Unscored items have Display

- **WHEN** no scoring runs (no ruleset found) and `ToSearchCompleted` is called
- **THEN** each item SHALL have Display set via `ReleaseDisplay.From(source.Title, fallbackMediaName)`
