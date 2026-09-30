## MODIFIED Requirements

### Requirement: TvSearchWorkerState produces search results via SceneRelease

The TvSearchWorkerState `ToSearchCompleted` method SHALL convert `EnrichedItem[]` to `SearchResultItem[]` by mapping each item through `SceneRelease.ForShow(item, MediaName)` and calling `Expand()`. Results SHALL be ordered by Score descending. The state SHALL NOT contain an `EnsureDisplay` method. The state SHALL NOT contain an `UnscoredItems` method that builds displays. All items SHALL have `Display` set after `Apply(ScoreCompleted)`.

#### Scenario: ToSearchCompleted uses SceneRelease

- **WHEN** ToSearchCompleted is called after scoring
- **THEN** each EnrichedItem SHALL be mapped via `SceneRelease.ForShow(item, MediaName).Expand()` and the results sorted by Score descending

#### Scenario: Display set for all items during scoring

- **WHEN** `Apply(ScoreCompleted)` processes items
- **THEN** ALL items (matched and unmatched) SHALL have a non-null Display using `ReleaseDisplay.From(source.Title, mediaName)` as the base, with ConstructedTitle overriding EpisodeTitle for matched items

#### Scenario: Unscored items have Display

- **WHEN** no scoring runs (no ruleset found) and `ToSearchCompleted` is called
- **THEN** each item SHALL have Display set via `ReleaseDisplay.From(source.Title, fallbackMediaName)`
