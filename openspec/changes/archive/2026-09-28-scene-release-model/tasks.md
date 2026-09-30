## 1. ReleaseDisplay.From factory

- [x] 1.1 Add static `From(string sourceTitle, string mediaName)` method to `ReleaseDisplay` that integrates CleanTitle logic (prefix strip, suffix strip, S##E## removal)
- [x] 1.2 Add unit tests for `ReleaseDisplay.From` covering: colon prefix, dash prefix, suffix strip, S##E## removal, case insensitivity, no-op when no match
- [x] 1.3 Replace all `ReleaseVariant.CleanTitle(...)` calls in `TvSearchWorkerState` and `MovieSearchWorkerState` with `ReleaseDisplay.From(...)`

## 2. Display always set (never null)

- [x] 2.1 In `TvSearchWorkerState.Apply(ScoreCompleted)`, set Display for ALL items (matched and unmatched) using `ReleaseDisplay.From` as base, ConstructedTitle overriding for matched
- [x] 2.2 In `MovieSearchWorkerState.Apply(ScoreCompleted)`, same: Display for all items
- [x] 2.3 In `TvSearchWorkerState`, replace `UnscoredItems()` to set Display via `ReleaseDisplay.From` instead of using `ReleaseVariant.CleanTitle`
- [x] 2.4 In `MovieSearchWorkerState`, same for `UnscoredItems()`
- [x] 2.5 Remove `EnsureDisplay()` from both WorkerStates
- [x] 2.6 Update WorkerState tests that asserted `Display == null` for unmatched items

## 3. SceneRelease record

- [x] 3.1 Create `SceneRelease.cs` in FunkArr.Search with record definition (MediaName, Identifier, EpisodeTitle, Item)
- [x] 3.2 Implement `ForShow(EnrichedItem item, string? mediaName)` with season/episode/airdate identifier resolution
- [x] 3.3 Implement `ForMovie(EnrichedItem item, string? mediaName)` with enriched year / broadcast year identifier resolution
- [x] 3.4 Implement `FormatTitle(int quality)` with Sanitize, MapQuality, CollapseDots as private helpers
- [x] 3.5 Implement `Expand()` producing `SearchResultItem[]` via VideoQuality.GetVariants, including Metadata mapping
- [x] 3.6 Write `SceneReleaseTests`: ForShow scenarios (season+episode, episode-only, airdate, no-data), ForMovie scenarios (enriched year, broadcast year fallback, no year)
- [x] 3.7 Write `SceneReleaseTests`: FormatTitle scenarios (full title, no identifier, special chars, umlauts, quality mapping, consecutive dots)
- [x] 3.8 Write `SceneReleaseTests`: Expand scenarios (3 URLs, 1 URL, 0 URLs, size estimation, metadata populated)

## 4. Wire up WorkerStates

- [x] 4.1 Replace `ReleaseVariant.Expand` + sort + `ToResultItem` in `TvSearchWorkerState.ToSearchCompleted` with `SceneRelease.ForShow(...).Expand()`
- [x] 4.2 Replace `ReleaseVariant.Expand` + sort + `ToResultItem` in `MovieSearchWorkerState.ToSearchCompleted` with `SceneRelease.ForMovie(...).Expand()`
- [x] 4.3 Update existing WorkerState tests to verify output titles match expected format

## 5. Remove old code

- [x] 5.1 Delete `src/FunkArr.Core/ReleaseTitleBuilder.cs`
- [x] 5.2 Delete `src/FunkArr.Search/ReleaseVariant.cs`
- [x] 5.3 Delete or migrate `src/FunkArr.Search.Tests/ReleaseTitleBuilderTests.cs` (scenarios already covered by 3.6/3.7)
- [x] 5.4 Delete or migrate `src/FunkArr.Search.Tests/ReleaseVariantTests.cs` (scenarios already covered by 3.8)
- [x] 5.5 Verify build succeeds with no references to deleted types
- [x] 5.6 Run all tests, verify 0 failures

## 6. Verify

- [x] 6.1 Run dotnet format, fix any violations
- [x] 6.2 Run architecture tests, verify no boundary violations
- [x] 6.3 Rebuild Docker, trigger Sonarr TV search, verify release title format unchanged
- [x] 6.4 Trigger Radarr movie search, verify enriched year appears in title (not broadcast year)
