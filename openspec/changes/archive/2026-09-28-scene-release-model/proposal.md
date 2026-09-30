## Why

The release title pipeline is scattered across two static helper classes
(`ReleaseTitleBuilder` in Core, `ReleaseVariant` in Search) that have no clear
ownership of the concept they implement. `ReleaseVariant` is simultaneously a
data record, a factory, a title builder, a title cleaner, and a DTO mapper.
`ReleaseTitleBuilder` is a bag of static string functions that only
`ReleaseVariant` calls. `CleanTitle` lives on `ReleaseVariant` but is called
six times from WorkerStates for display resolution that has nothing to do with
release variants. The `BuildIdentifier` method uses a 5-tuple pattern match
with 10 wildcards mixing Show and Movie logic in a single switch.

This makes the code hard to follow, hard to extend (the movie-year bug was a
direct consequence), and hard to test in isolation.

## What Changes

- **Replace `ReleaseVariant` record + `ReleaseTitleBuilder` static class with a
  `SceneRelease` record** that captures the three resolved fields (MediaName,
  Identifier, EpisodeTitle) alongside the source `EnrichedItem`, with instance
  methods `FormatTitle(quality)` and `Expand()` that produce `SearchResultItem[]`.
- **Split construction into `ForShow` / `ForMovie` factory methods** instead of
  a single `Expand` with a MediaType parameter. Each factory is a clear,
  linear 3-5 line method with no wildcards or tuple matching.
- **Move `CleanTitle` onto `ReleaseDisplay` as `ReleaseDisplay.From(title, mediaName)`**
  so display resolution logic lives where the display record is defined.
  WorkerStates call `ReleaseDisplay.From(...)` instead of
  `ReleaseVariant.CleanTitle(...)`.
- **Set Display on all items during scoring** (never null), eliminating
  `EnsureDisplay()` and the fallback in `Expand()`.
- **Remove `ReleaseTitleBuilder` from Core.** Its formatting helpers
  (Sanitize, MapQuality, CollapseDots) become private methods on
  `SceneRelease`. No other project uses them.
- **Remove `ReleaseVariant` entirely.** Its responsibilities are absorbed by
  `SceneRelease` (title building, quality expansion, result mapping) and
  `ReleaseDisplay` (title cleaning).

## Capabilities

### New Capabilities

- `scene-release`: The `SceneRelease` record model, its factory methods
  (`ForShow`, `ForMovie`), instance methods (`FormatTitle`, `Expand`), and
  the integration with WorkerStates. Replaces `release-title-format` specs
  for `ReleaseTitleBuilder` and the `ReleaseVariant.Expand` / `BuildIdentifier`
  contracts.

### Modified Capabilities

- `release-display`: `ReleaseDisplay` gains a `From(sourceTitle, mediaName)`
  factory method that integrates `CleanTitle` logic. Display is always set
  (never null on `EnrichedItem`) after scoring, eliminating `EnsureDisplay`.
- `movie-search`: `MovieSearchWorkerState.ToSearchCompleted` uses
  `SceneRelease.ForMovie` instead of `ReleaseVariant.Expand`. `EnsureDisplay`
  removed.
- `tv-search`: `TvSearchWorkerState.ToSearchCompleted` uses
  `SceneRelease.ForShow` instead of `ReleaseVariant.Expand`. `EnsureDisplay`
  removed.

## Impact

- **Removed files:** `src/FunkArr.Core/ReleaseTitleBuilder.cs`,
  `src/FunkArr.Search/ReleaseVariant.cs`
- **New file:** `src/FunkArr.Search/SceneRelease.cs`
- **Modified:** `ReleaseDisplay.cs` (add `From` factory),
  `TvSearchWorkerState.cs`, `MovieSearchWorkerState.cs` (use SceneRelease,
  always set Display, remove EnsureDisplay/UnscoredItems fallbacks)
- **Tests:** `ReleaseTitleBuilderTests.cs` and `ReleaseVariantTests.cs`
  replaced by `SceneReleaseTests.cs`. WorkerState tests updated for
  non-null Display.
- **No API changes.** `SearchResultItem` (Messages boundary) is unchanged.
  Newznab XML output is unchanged. Title format is unchanged.
